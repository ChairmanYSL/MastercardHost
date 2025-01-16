import os
import json

# 需要处理的目录路径
directory_path = "./Config"

for filename in os.listdir(directory_path):
    if filename.endswith(".json"):
        file_path = os.path.join(directory_path, filename)

        # 读取JSON文件
        with open(file_path, "r", encoding="utf-8") as f:
            data = json.load(f)

        # 确认AIDParam字段存在且是数组
        if "AIDParam" in data and isinstance(data["AIDParam"], list):
            changed = False
            for item in data["AIDParam"]:
                # item是AIDParam下的每个对象
                # 判断9F2A是否已经存在
                if item.get("9F06") == "A0000000041010" and item.get("9C") == "00":
                    item["9F1D"] = "6CFF000000000000"
                    changed = True
                elif item.get("9F06") == "A0000000041010" and item.get("9C") == "09":
                    item["9F1D"] = None
                    changed = True
                elif item.get("9F06") == "A0000000043060":
                    item["9F1D"] = "44FF800000000000"
                    changed = True


            # 如果有修改则写回文件
            if changed:
                with open(file_path, "w", encoding="utf-8") as f:
                    json.dump(data, f, ensure_ascii=False, indent=4)
                print(f"Updated {filename}")

        else:
            # 不存在AIDParam或其不是一个列表则跳过
            print(f"No AIDParam array found in {filename}, skipped.")
