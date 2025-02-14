import socket
import sys
import os

def start_server():
    host = 'localhost'
    
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server_socket:
        server_socket.bind((host, 65234))
        actual_port = server_socket.getsockname()[1]
        server_socket.listen()

        print(f"Server listening on {host}:{actual_port}")
        conn, addr = server_socket.accept()
        with conn:
            print(f"Connected on {addr}")
            while True:
                data = conn.recv(1024)
                if not data:
                    break

                print("Received:", data.decode())

if __name__ == "__main__":
    start_server()
