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

class AiderRequestHeader:
    HEADER_SIZE = 8
    
    def __init__(self, header_marker: int, content_length: int):
        self.header_marker = header_marker
        self.content_length = content_length

    @classmethod
    def deserialize(cls, data: bytes):
        header_marker = struct.unpack('<i', data[0:4])[0]
        if (header_marker != 987654321):
            return None
        
        content_length = struct.unpack('<i', data[4:8])[0]

        return cls(header_marker, content_length)

class AiderRequest:
    def __init__(self, content: str):
        self.header = None
        self.content = content
    
    # see Interface.cs for the serialization function
    @classmethod
    def deserialize(cls, data: bytes, header: AiderRequestHeader) -> 'AiderRequest':
        if (data is None or len(data) < header.content_length):
            raise ValueError("Invalid data length")
        
        content = data.decode()
        print(f"Deserialized request: {content}")

        return cls(content)
    
    def get_command_string(self) -> str:
        name = ""
        if self.content.strip().startswith("/"):
            name = self.content.strip().split(" ")[0].upper().replace("/", "").replace("-", "_")

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
    def __init__(self, content: str, last: bool, is_diff: bool = False, error: bool = False, tokensSent: int = 0, tokensReceived: int = 0, messageCost: float = 0, sessionCost: float = 0):
        self.content = content
        self.last = last
        self.is_diff = is_diff
        self.error = error
        self.tokensSent = tokensSent
        self.tokensReceived = tokensReceived
        self.messageCost = messageCost
        self.sessionCost = sessionCost

    def serialize(self) -> bytes:
        msg = struct.pack('<i', 123456789) + struct.pack('<i', len(self.content)) + struct.pack('<?', self.last) + struct.pack('<?', self.is_diff) + struct.pack('<?', self.error) + struct.pack('<i', self.tokensSent) + struct.pack('<i', self.tokensReceived) + struct.pack('<f', self.messageCost) + struct.pack('<f', self.sessionCost) + self.content.encode()
        return msg

