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

class AiderRequest:
    def __init__(self, command: AiderCommand, content: str):
        self.command = command
        self.content = content
    
    # see Interface.cs for the serialization function
    @classmethod
    def deserialize(cls, data: bytes) -> 'AiderRequest':
        command = struct.unpack('<i', data[0:4])[0]
        content_length = struct.unpack('<i', data[4:8])[0]
        content = data[8:8+content_length].decode()

        return cls(AiderCommand(command), content)
    
class AiderResponse:
    def __init__(self, content: str, part: int, last: bool):
        self.content = content
        self.part = part
        self.last = last

    def serialize(self) -> bytes:
        return struct.pack('<i', len(self.content)) + self.content.encode() + struct.pack('<i', self.part) + struct.pack('<?', self.last)
    