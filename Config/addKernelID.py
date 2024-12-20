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
                if "9F2A" not in item:
                    item["9F2A"] = "02"
                    changed = True

            # 如果有修改则写回文件
            if changed:
                with open(file_path, "w", encoding="utf-8") as f:
                    json.dump(data, f, ensure_ascii=False, indent=4)
                print(f"Updated {filename}")

        else:
            # 不存在AIDParam或其不是一个列表则跳过
            print(f"No AIDParam array found in {filename}, skipped.")
