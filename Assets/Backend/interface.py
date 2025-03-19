from enum import IntEnum
import struct

class AiderCommand(IntEnum):
    NONE = -1
    ADD = 0
    ARCHITECT = 1
    ASK = 2
    CHAT_MODE = 3
    CLEAR = 4
    CODE = 5
    COMMIT = 6
    DROP = 7
    LINT = 8
    LOAD = 9
    LS = 10
    MAP = 11
    MAP_REFRESH = 12
    READ_ONLY = 13
    RESET = 14
    UNDO = 15
    WEB = 16
    UNKNOWN = 17


class AiderRequest:
    def __init__(self, content: str):
        self.content = content
    
    # see Interface.cs for the serialization function
    @classmethod
    def deserialize(cls, data: bytes) -> 'AiderRequest':
        content_length = struct.unpack('<i', data[0:4])[0]
        content = data[4:4+content_length].decode()
        print(f"Deserialized request: {content}")

        return cls(content)
    
    def get_command_string(self) -> str:
        name = ""
        if self.content.strip().startswith("/"):
            name = self.content.strip().split(" ")[0].upper().replace("/", "")

        return name
    
    def get_command(self) -> AiderCommand:
        command = AiderCommand.NONE
        if self.content.strip().startswith("/"):
            command_name = self.get_command_string()
            if hasattr(AiderCommand, command_name):
                command = AiderCommand[command_name]
            else:
                print(f"Command {command_name} not found")
                command = AiderCommand.UNKNOWN

        return command
    
    def strip_command(self) -> str:
        content = self.content
        if self.content.strip().startswith("/"):
            content = " ".join(self.content.strip().split(" ")[1:])
        
        return content
    
class AiderResponse:
    def __init__(self, content: str, last: bool = False, error: bool = False):
        self.content = content
        self.last = last
        self.error = error

    def serialize(self) -> bytes:
        return struct.pack('<i', len(self.content)) + self.content.encode() + struct.pack('<?', self.last) + struct.pack('<?', self.error)