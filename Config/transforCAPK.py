import xml.etree.ElementTree as ET
import json
import os

def validate_xml(file_path):
    try:
        tree = ET.parse(file_path)
        print(f"XML file '{file_path}' is well-formed.")
    except ET.ParseError as e:
        print(f"XML file '{file_path}' is not well-formed.")
        print(f"Error: {e}")

def remove_bom(file_path):
    with open(file_path, 'rb') as f:
        content = f.read()
    # Check for BOM and remove it if present
    if content.startswith(b'\xef\xbb\xbf'):
        content = content[3:]
        with open(file_path, 'wb') as f:
            f.write(content)

# 定义从XML文件加载和解析数据的函数
def parse_xml_file(file_path):
    remove_bom(file_path)  # Remove BOM if present
    tree = ET.parse(file_path)
    root = tree.getroot()
    
    capks = []
    
    for capk in root.findall('CAPK'):
        json_dict = {}
        for cfg in capk.findall('Cfg'):
            label = cfg.get('label')
            value = cfg.text
            if label:
                json_dict[label] = value
        capks.append(json_dict)
    
    return capks

# 定义重新格式化JSON数据的函数
def reformat_json_data(capk_data):
    formatted_data = {
        "CAPKParam": capk_data
    }
    return formatted_data

# 定义将JSON数据写入文件的函数
def write_json_to_file(data, file_path):
    with open(file_path, 'w', encoding='utf-8') as json_file:
        json.dump(data, json_file, indent=4, ensure_ascii=False)

# 遍历并处理子目录中的文件
def process_subdirectory(capk_subdirectory):
    # 获取当前目录
    current_directory = os.getcwd()
    capk_path = os.path.join(current_directory, capk_subdirectory)
    
    # 获取AID目录和SimData目录中的文件名
    capk_files = set(os.listdir(capk_path))
        
    for file in capk_files:
        capk_file_path = os.path.join(capk_path, file)
        print(f"Processing files: {capk_file_path}")
        
        try:
            # 检查XML文件合法性
            validate_xml(capk_file_path)
            
            # 解析XML文件并转换为JSON数据
            capk_data = parse_xml_file(capk_file_path)
            
            # 重新格式化JSON数据
            formatted_json_data = reformat_json_data(capk_data)
            
            # 获取XML文件名（不包含目录和扩展名）
            xml_filename = os.path.basename(capk_file_path)
            json_filename = os.path.splitext(xml_filename)[0] + '.json'
            
            # 目标JSON文件目录和路径
            parent_directory = os.path.dirname(os.path.dirname(capk_file_path))
            json_directory = os.path.join(parent_directory, 'CAPK')
            json_file_path = os.path.join(json_directory, json_filename)
            print(f"Target json file: {json_file_path}")
            
            # 创建目标目录（如果不存在）
            os.makedirs(json_directory, exist_ok=True)
            
            # 将重新格式化的JSON数据写入文件
            write_json_to_file(formatted_json_data, json_file_path)
        
        except ET.ParseError as e:
            print(f"Failed to parse XML file: {capk_file_path}")
            print(f"Error: {e}")

# 处理AID和SimData目录
process_subdirectory('CAPK')

print("All data has been processed and written to JSON files.")