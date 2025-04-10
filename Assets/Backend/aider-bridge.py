import os
import random
import socket
import time
import aider_main
from interface import AiderCommand, AiderRequest, AiderResponse

reply = """
I'll create a series of `executeCode` commands to test various Unity built-in types. Each command will be in its own block to help identify which types are accessible.

```unity
{
    "command": "executeCode", 
    "shortDescription": "Testing basic Unity types", 
    "code": "Debug.Log(\\"Testing execute!\\")"
}
```

"""

class Server:
    def __init__(self):
        self.host = 'localhost'
        self.port = 65234
        self.server_socket = None
        self.conn = None
        self.addr = None

    def start(self):
        self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.server_socket.bind((self.host, self.port))
        actual_port = self.server_socket.getsockname()[1]
        self.server_socket.listen()

        print(f"Server listening on {self.host}:{actual_port}")
        self.conn, self.addr = self.server_socket.accept()
        print(f"Connected on {self.addr}")

    def send(self, message: AiderResponse):
        msg = message.serialize()
        self.conn.sendall(msg)

    def send_string(self, string: str):
        self.send(AiderResponse(string, True))

    def send_error(self, string: str):
        self.send(AiderResponse(string, True, True))

    def simulate_reply(self, string: str):
        words = string.split(" ")
        for i, word in enumerate(words):
            if i < len(words) - 1:
                print(word + " ", end="", flush=True)
                self.send(AiderResponse(word + " "))
                time.sleep(random.uniform(0.01, 0.2))
            else:
                self.send(AiderResponse(word, True))

    def receive(self):
        data = self.conn.recv(1024)
        if not data:
            return None
        return AiderRequest.deserialize(data)

    def close(self):
        self.conn.close()
        self.server_socket.close()

    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close()

def main():
    aider_main.init()
    with Server() as server:
        while True: # continue to listen for new connections
            server.start()

            while True: # continue to listen for new messages
                request = server.receive()
                print(f"Received request: {request}")

                if request is None:
                    break

                command = request.get_command()
                command_name = request.get_command_string()
                print(f"Received command: {command_name}")

                coder = aider_main.coder

                match command:
                    case AiderCommand.UNKNOWN:
                        server.send_error(f"The command {command_name} is not recognized.")
                        continue
                    case AiderCommand.LS:
                        server.send_string("\n".join(coder.abs_fnames))
                        continue
                    case AiderCommand.ADD:
                        name = coder.get_rel_fname(request.strip_command())
                        if os.path.exists(name):
                            coder.add_rel_fname(name) 
                            server.send_string(f"Added {name}")
                        else:

                            # check if there is only one file in all files that ends with filename
                            # because the user may have just but the name of the file not the path
                            filename = name.replace("\\", "/").split("/")[-1]
                            matches = [fname for fname in coder.get_all_relative_files() if fname.endswith(f"{filename}")]
                            if len(matches) == 1:
                                coder.add_rel_fname(matches[0])
                                server.send_string(f"Added {matches[0]} implicitly.")
                            else:
                                server.send_error(f"Cannot add {name} because it does not exist.")

                        continue
                    case AiderCommand.DROP:
                        name = coder.get_rel_fname(request.strip_command())
                        if name in coder.get_inchat_relative_files():
                            coder.drop_rel_fname(name) 
                            server.send_string(f"Dropped {name}")
                        else:
                            
                            # do the same for drop as we did for add
                            filename = name.replace("\\", "/").split("/")[-1]
                            matches = [fname for fname in coder.get_inchat_relative_files() if fname.endswith(f"{filename}")]
                            if len(matches) == 1:
                                coder.drop_rel_fname(matches[0])
                                server.send_string(f"Dropped {matches[0]} implicitly.")
                            else:
                                server.send_error(f"Cannot drop {name} because it is not in chat.")

                        continue
                    case AiderCommand.RESET:
                        coder.abs_fnames = set()
                        coder.abs_read_only_fnames = set()
                        coder.done_messages = []
                        coder.cur_messages = []
                        server.send_string("Reset chat successfully.")
                        continue 

                full_output = ""
                for output in aider_main.send_message_get_output(request.content):
                    full_output += output
                    server.send(AiderResponse(full_output, False, False))

                server.send(AiderResponse(full_output, True, False))
                
                # server.send(AiderResponse(reply, True, False))
                # print("Reply sent.")

if __name__ == "__main__":
    main()
