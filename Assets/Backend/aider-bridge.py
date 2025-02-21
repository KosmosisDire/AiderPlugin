import socket
import sys
import os
import json
from aider.chat import Coder

#Initialize Aider Coder Instance
coder = Coder()


def handle_message(message:str) -> str:
 """
	handles incoming messages, decodes them, processes w/ Aider, and returns a response.
	 both raw text/ Json- formatted as well"""

	try:
	   # Try parsing messages as JSON
	   data = json.loads(message)
	   user_input = data.get("text","").strip()
	except json.JSONDecodeError:
	  #If not JSON, treat as raw text msg
	  user_input = message.strip()

	if not user_input:
	    return "Error: No valid message received."

	print(f"Processing message: {user_input}")

	# Send input to Aider and get response = coder.chat(user_input)

	aider_response = coder.chat(user_input)

	return aider_reponse


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
