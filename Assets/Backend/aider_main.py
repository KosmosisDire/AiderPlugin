import io as stdio
import os
import sys
from aider.coders import Coder
from aider.models import Model
from aider.io import InputOutput
import git
from aider.args import get_parser
from pathlib import Path
from dotenv import load_dotenv
from aider.repo import ANY_GIT_ERROR, GitRepo
import aider.coders as coders
from unity_coder import UnityCoder
from aider.watch import FileWatcher
from aider.history import ChatSummary

total_cost = 0.0
message_cost = 0.0
tokens_sent = 0
tokens_received = 0

def check_config_files_for_yes(config_files):
    found = False
    for config_file in config_files:
        if Path(config_file).exists():
            try:
                with open(config_file, "r") as f:
                    for line in f:
                        if line.strip().startswith("yes:"):
                            print("Configuration error detected.")
                            print(f"The file {config_file} contains a line starting with 'yes:'")
                            print("Please replace 'yes:' with 'yes-always:' in this file.")
                            found = True
            except Exception:
                pass
    return found

def get_git_root():
    """Try and guess the git repo, since the conf.yml can be at the repo root"""
    try:
        repo = git.Repo(search_parent_directories=True)
        return repo.working_tree_dir
    except (git.InvalidGitRepositoryError, FileNotFoundError):
        return None

def generate_search_path_list(default_file, git_root, command_line_file):
    files = []
    files.append(Path.home() / default_file)  # homedir
    if git_root:
        files.append(Path(git_root) / default_file)  # git root
    files.append(default_file)
    if command_line_file:
        files.append(command_line_file)

    resolved_files = []
    for fn in files:
        try:
            resolved_files.append(Path(fn).resolve())
        except OSError:
            pass

    files = resolved_files
    files.reverse()
    uniq = []
    for fn in files:
        if fn not in uniq:
            uniq.append(fn)
    uniq.reverse()
    files = uniq
    files = list(map(str, files))
    files = list(dict.fromkeys(files))

    return files

def load_dotenv_files(git_root, dotenv_fname, encoding="utf-8"):
    dotenv_files = generate_search_path_list(
        ".env",
        git_root,
        dotenv_fname,
    )
    loaded = []
    for fname in dotenv_files:
        try:
            if Path(fname).exists():
                load_dotenv(fname, override=True, encoding=encoding)
                loaded.append(fname)
        except OSError as e:
            print(f"OSError loading {fname}: {e}")
        except Exception as e:
            print(f"Error loading {fname}: {e}")
    return loaded

def guessed_wrong_repo(io, git_root, fnames, git_dname):
    """After we parse the args, we can determine the real repo. Did we guess wrong?"""

    try:
        check_repo = Path(GitRepo(io, fnames, git_dname).root).resolve()
    except (OSError,) + ANY_GIT_ERROR:
        return

    # we had no guess, rely on the "true" repo result
    if not git_root:
        return str(check_repo)

    git_root = Path(git_root).resolve()
    if check_repo == git_root:
        return

    return str(check_repo)


def check_gitignore(git_root, io, ask=True):
    if not git_root:
        return

    try:
        repo = git.Repo(git_root)
        patterns_to_add = []

        if not repo.ignored(".aider"):
            patterns_to_add.append(".aider*")

        env_path = Path(git_root) / ".env"
        if env_path.exists() and not repo.ignored(".env"):
            patterns_to_add.append(".env")

        if not patterns_to_add:
            return

        gitignore_file = Path(git_root) / ".gitignore"
        if gitignore_file.exists():
            try:
                content = io.read_text(gitignore_file)
                if content is None:
                    return
                if not content.endswith("\n"):
                    content += "\n"
            except OSError as e:
                io.tool_error(f"Error when trying to read {gitignore_file}: {e}")
                return
        else:
            content = ""
    except ANY_GIT_ERROR:
        return

    if ask:
        io.tool_output("You can skip this check with --no-gitignore")
        if not io.confirm_ask(f"Add {', '.join(patterns_to_add)} to .gitignore (recommended)?"):
            return

    content += "\n".join(patterns_to_add) + "\n"

    try:
        io.write_text(gitignore_file, content)
        io.tool_output(f"Added {', '.join(patterns_to_add)} to .gitignore")
    except OSError as e:
        io.tool_error(f"Error when trying to write to {gitignore_file}: {e}")
        io.tool_output(
            "Try running with appropriate permissions or manually add these patterns to .gitignore:"
        )
        for pattern in patterns_to_add:
            io.tool_output(f"  {pattern}")


def make_new_repo(git_root, io):
    try:
        repo = git.Repo.init(git_root)
        check_gitignore(git_root, io, False)
    except ANY_GIT_ERROR as err:  # issue #1233
        io.tool_error(f"Unable to create git repo in {git_root}")
        io.tool_output(str(err))
        return

    io.tool_output(f"Git repository created in {git_root}")
    return repo

def setup_git(git_root, io):
    if git is None:
        return

    try:
        cwd = Path.cwd()
    except OSError:
        cwd = None

    repo = None

    if git_root:
        try:
            repo = git.Repo(git_root)
        except ANY_GIT_ERROR:
            pass
    elif cwd == Path.home():
        io.tool_warning(
            "You should probably run aider in your project's directory, not your home dir."
        )
        return
    elif cwd and io.confirm_ask(
        "No git repo found, create one to track aider's changes (recommended)?"
    ):
        git_root = str(cwd.resolve())
        repo = make_new_repo(git_root, io)

    if not repo:
        return

    try:
        user_name = repo.git.config("--get", "user.name") or None
    except git.exc.GitCommandError:
        user_name = None

    try:
        user_email = repo.git.config("--get", "user.email") or None
    except git.exc.GitCommandError:
        user_email = None

    if user_name and user_email:
        return repo.working_tree_dir

    with repo.config_writer() as git_config:
        if not user_name:
            git_config.set_value("user", "name", "Your Name")
            io.tool_warning('Update git name with: git config user.name "Your Name"')
        if not user_email:
            git_config.set_value("user", "email", "you@example.com")
            io.tool_warning('Update git email with: git config user.email "you@example.com"')

    return repo.working_tree_dir


coder: Coder = None
def init(argv=None, force_git_root=None):
    """
    Initialize the coder. Use the send_message_get_output function to send messages to the coder.
    dry_run: If True, the coder will not modify any files only output reply.
    """
    global coder

    if argv is None:
            argv = sys.argv[1:]

    if git is None:
        git_root = None
    elif force_git_root:
        git_root = force_git_root
    else:
        git_root = get_git_root()

    conf_fname = Path(".aider.conf.yml")

    default_config_files = []
    try:
        default_config_files += [conf_fname.resolve()]  # CWD
    except OSError:
        pass

    if git_root:
        git_conf = Path(git_root) / conf_fname  # git root
        if git_conf not in default_config_files:
            default_config_files.append(git_conf)
    default_config_files.append(Path.home() / conf_fname)  # homedir
    default_config_files = list(map(str, default_config_files))

    parser = get_parser(default_config_files, git_root)
    try:
        args, unknown = parser.parse_known_args(argv)
    except AttributeError as e:
        if all(word in str(e) for word in ["bool", "object", "has", "no", "attribute", "strip"]):
            if check_config_files_for_yes(default_config_files):
                return 1
        raise e

    if args.verbose:
        print("Config files search order, if no --config:")
        for file in default_config_files:
            exists = "(exists)" if Path(file).exists() else ""
            print(f"  - {file} {exists}")

    default_config_files.reverse()

    parser = get_parser(default_config_files, git_root)

    args, unknown = parser.parse_known_args(argv)

    # Load the .env file specified in the arguments
    load_dotenv_files(git_root, args.env_file, args.encoding)

    # Parse again to include any arguments that might have been defined in .env
    args = parser.parse_args(argv)

    if git is None:
        args.git = False

    io = InputOutput(
        yes=True,
        pretty=False # this is important to allow intercepting the output rather than processing through the markdown stream!
        )

    if args.api_key:
        for api_setting in args.api_key:
            try:
                provider, key = api_setting.split("=", 1)
                env_var = f"{provider.strip().upper()}_API_KEY"
                os.environ[env_var] = key.strip()
            except ValueError:
                io.tool_error(f"Invalid --api-key format: {api_setting}")
                io.tool_output("Format should be: provider=key")
                return 1

    if args.anthropic_api_key:
        os.environ["ANTHROPIC_API_KEY"] = args.anthropic_api_key
    if args.openai_api_key:
        os.environ["OPENAI_API_KEY"] = args.openai_api_key
    if args.openai_api_base:
        os.environ["OPENAI_API_BASE"] = args.openai_api_base
    

    all_files = args.files + (args.file or [])
    fnames = [str(Path(fn).resolve()) for fn in all_files]
    read_only_fnames = []
    for fn in args.read or []:
        path = Path(fn).expanduser().resolve()
        if path.is_dir():
            read_only_fnames.extend(str(f) for f in path.rglob("*") if f.is_file())
        else:
            read_only_fnames.append(str(path))

    if len(all_files) > 1:
        good = True
        for fname in all_files:
            if Path(fname).is_dir():
                io.tool_error(f"{fname} is a directory, not provided alone.")
                good = False
        if not good:
            io.tool_output(
                "Provide either a single directory of a git repo, or a list of one or more files."
            )
            return 1

    git_dname = None
    if len(all_files) == 1:
        if Path(all_files[0]).is_dir():
            if args.git:
                git_dname = str(Path(all_files[0]).resolve())
                fnames = []
            else:
                io.tool_error(f"{all_files[0]} is a directory, but --no-git selected.")
                return 1

    if args.git and not force_git_root and git is not None:
        right_repo_root = guessed_wrong_repo(io, git_root, fnames, git_dname)
        if right_repo_root:
            return init(argv, right_repo_root)
        
    if args.git:
        git_root = setup_git(git_root, io)
        if args.gitignore:
            check_gitignore(git_root, io)

    if (args.dry_run):
        print("Dry run mode enabled. No files will be modified!")

    if args.cache_prompts and args.map_refresh == "auto":
        args.map_refresh = "files"

    model = Model(model=args.model)

    summarizer = ChatSummary(
        [model.weak_model, model],
        args.max_chat_history_tokens or model.max_chat_history_tokens,
    )

    repo = None
    if args.git:
        try:
            repo = GitRepo(
                io,
                fnames,
                git_dname,
                args.aiderignore,
                models=model.commit_message_models(),
                attribute_author=args.attribute_author,
                attribute_committer=args.attribute_committer,
                attribute_commit_message_author=args.attribute_commit_message_author,
                attribute_commit_message_committer=args.attribute_commit_message_committer,
                commit_prompt=args.commit_prompt,
                subtree_only=args.subtree_only,
            )
        except FileNotFoundError:
            pass


    coders.__all__.append(UnityCoder)
    print(args.edit_format)
    coder = Coder.create(
        main_model=model,
        edit_format="unity",
        fnames=fnames, 
        io=io,
        repo=repo,
        use_git=True,
        summarizer=summarizer,
        chat_language=args.chat_language,
        cache_prompts=args.cache_prompts,
        map_refresh=args.map_refresh,
        map_tokens=args.map_tokens,
        verbose=args.verbose,
        dry_run=args.dry_run, # a dry run will cause it to not modify files
        stream=True)
    
    ignores = []
    if git_root:
        ignores.append(str(Path(git_root) / ".gitignore"))
    if args.aiderignore:
        ignores.append(args.aiderignore)

    if args.watch_files:
        file_watcher = FileWatcher(
            coder,
            gitignores=ignores,
            verbose=args.verbose,
            root=str(Path.cwd()) if args.subtree_only else None,
        )
        coder.file_watcher = file_watcher
    
    # calling this forces a repo map update at the start
    print("Initializing coder...")
    coder.format_messages()
    print("Coder initialized.")

    # monkey patch function to extract the usage report before it is cleared
    original_usage_report = coder.show_usage_report
    def show_usage_report():
        global total_cost, message_cost, tokens_sent, tokens_received
        global coder
        total_cost = coder.total_cost
        message_cost = coder.message_cost
        tokens_sent = coder.message_tokens_sent
        tokens_received = coder.message_tokens_received

        original_usage_report()
        
    coder.show_usage_report = show_usage_report


def send_message_get_output(message):
    """
    This function runs a command and returs the output in async chunks. In order to process these chunks run something like this:

    ```python
    for output in send_message_get_output("Hello"):
        # `output` is only a small piece of the text
        # this code will run in real time as new chunks are read from the LLM:
        handle_output_chunk(output) 
    ```

    """

    global coder
    coder.init_before_message()
    message = coder.preproc_user_input(message)
    coder.reflected_message = None
    if (message is None or len(message) == 0):
        print("Empty message, nothing to do.")
        return "..."
    
    yield from coder.send_message(message)

if __name__ == "__main__":
    init()
    for out in send_message_get_output("Hello"):
        print("\nTest Output:", out)
