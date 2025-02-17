import socket
import subprocess

def process_message_with_aider(message):
    #Runs Aider with the message and returns its response
    try:
        result = subprocess.run(
            ["python", "aider-bridge.py", message],  # aider-bridge.py handles this
            capture_output=True,
            text=True
        )
        return result.stdout.strip() if result.stdout else "No response from Aider"
    except Exception as e:
        return f"Error processing message with Aider: {str(e)}"

def start_server():
    host = '127.0.0.1'  # should match the Unity client
    port = 65234        # should match Unity client's connection

    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server_socket:
        server_socket.bind((host, port))
        server_socket.listen()
        print(f"Server listening on {host}:{port}")

        conn, addr = server_socket.accept()
        with conn:
            print(f"Connected by {addr}")
            while True:
                data = conn.recv(1024)
                if not data:
                    break

                message = data.decode().strip()
                print("Received:", message)

                # Process message with Aider if needed
                if "Aider" in message.upper():
                    response = process_message_with_aider(message)
                else:
                    response = f"Echo: {message}"

                conn.sendall(response.encode())  # Send response back to client

if __name__ == "__main__":
    start_server()
