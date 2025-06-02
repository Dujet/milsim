# YOLOv8 Inference Server

This is a lightweight Flask-based Python backend that runs YOLOv8 object detection on images received from a Unity application. The server loads a trained YOLOv8 model and exposes a single HTTP endpoint for inference.

## 🔧 Requirements

- Python 3.8+
- pip
- virtualenv (recommended)

## 📦 Setup

1. Navigate to the folder:
	cd inference_server

2. (Optional) Create and activate a virtual environment:
	python -m venv ../inference_env
	source ../inference_env/bin/activate  # Windows: ../inference_env/Scripts/activate

3. Install dependencies
	pip install -r requirements.txt

4. Run the server
	python inference_server.py

## 📝 Notes
- Make sure Unity and the Python server run on the same machine or allow network access.




