# QuantLab> python -s src/strategies/api/grpc/compile_proto.py
import os
from grpc_tools import protoc

def main():
    # Determine project root (two levels up from this file)
    root_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../.."))
    proto_path = os.path.join(root_dir, "src")
    proto_file = os.path.join(proto_path, "strategies/api/grpc/strategies.proto")

    print(f"Compiling proto: {proto_file}")

    protoc.main([
        "grpc_tools.protoc",
        f"-I={proto_path}",
        f"--python_out={proto_path}",
        f"--grpc_python_out={proto_path}",
        proto_file,
    ])

if __name__ == "__main__":
    main()
