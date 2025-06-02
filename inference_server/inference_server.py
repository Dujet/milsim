from flask import Flask, request, jsonify
from ultralytics import YOLO
import cv2
import numpy as np
import base64

app = Flask(__name__)
model = YOLO("best.pt")  # Your YOLOv8 model

@app.route("/detect", methods=["POST"])
def detect():
    try:
        data = request.json
        img_data = base64.b64decode(data["image"])
        img = cv2.imdecode(np.frombuffer(img_data, np.uint8), cv2.IMREAD_COLOR)

        results = model(img, conf=0.7)[0]
        detections = []

        for box in results.boxes:
            x1, y1, x2, y2 = box.xyxy[0].tolist()
            conf = box.conf[0].item()
            cls = int(box.cls[0].item())
            detections.append({
                "class": cls,
                "confidence": round(conf, 4),
                "bbox": [round(x1), round(y1), round(x2), round(y2)]
            })

        return jsonify({"detections": detections})
    
    except Exception as e:
        return jsonify({"error": str(e)})

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000)
