import random
import socket
import time
from interface import *

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
        self.conn.sendall(message.serialize())

    def simulate_reply(self, string: str):
        words = string.split(" ")
        for i, word in enumerate(words):
            if i < len(words) - 1:
                self.send(AiderResponse(word + " ", i, False))
                time.sleep(random.uniform(0.01, 0.2))
            else:
                self.send(AiderResponse(word, i, True))

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
    with Server() as server:
        server.start()

        while True:
            request = server.receive()

            if request is None:
                break

            response = "Hello there, my name is Aider. I am here to help you with your code!"
            response += "\nThe message you sent me was: \n\t" + request.content

            server.simulate_reply(response)

if __name__ == "__main__":
    main()
