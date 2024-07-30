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
    
    kernel_aid_combinations = []
    terminal_parameters = {}
    
    for kernel in root.findall('KernelAidCombination'):
        json_dict = {}
        for cfg in kernel.findall('Cfg'):
            label = cfg.get('label')
            value = cfg.text
            if label:
                json_dict[label] = value
        kernel_aid_combinations.append(json_dict)
    
    for terminal in root.findall('TerminalParameter'):
        for cfg in terminal.findall('Cfg'):
            label = cfg.get('label')
            value = cfg.text
            if label:
                terminal_parameters[label] = value
    
    return kernel_aid_combinations, terminal_parameters

# 定义重新格式化JSON数据的函数
def reformat_json_data(aid_data, term_data):
    formatted_data = {
        "AIDParam": aid_data,
        "TermParam": term_data
    }
    return formatted_data

# 定义将JSON数据写入文件的函数
def write_json_to_file(data, file_path):
    with open(file_path, 'w', encoding='utf-8') as json_file:
        json.dump(data, json_file, indent=4, ensure_ascii=False)

# 遍历并处理子目录中的文件
def process_subdirectory(aid_subdirectory, simdata_subdirectory):
    # 获取当前目录
    current_directory = os.getcwd()
    aid_path = os.path.join(current_directory, aid_subdirectory)
    simdata_path = os.path.join(current_directory, simdata_subdirectory)
    
    # 获取AID目录和SimData目录中的文件名
    aid_files = set(os.listdir(aid_path))
    simdata_files = set(os.listdir(simdata_path))
    
    # 找到两个目录中同名的文件
    common_files = aid_files.intersection(simdata_files)
    
    for file in common_files:
        aid_file_path = os.path.join(aid_path, file)
        simdata_file_path = os.path.join(simdata_path, file)
        print(f"Processing files: {aid_file_path} and {simdata_file_path}")
        
        try:
            # 检查XML文件合法性
            validate_xml(aid_file_path)
            validate_xml(simdata_file_path)
            
            # 解析XML文件并转换为JSON数据
            aid_data, _ = parse_xml_file(aid_file_path)
            _, term_data = parse_xml_file(simdata_file_path)
            
            # 重新格式化JSON数据
            formatted_json_data = reformat_json_data(aid_data, term_data)
            
            # 获取XML文件名（不包含目录和扩展名）
            xml_filename = os.path.basename(aid_file_path)
            json_filename = os.path.splitext(xml_filename)[0] + '.json'
            
            # 目标JSON文件目录和路径
            parent_directory = os.path.dirname(os.path.dirname(aid_file_path))
            json_directory = os.path.join(parent_directory, 'Config')
            json_file_path = os.path.join(json_directory, json_filename)
            print(f"Target json file: {json_file_path}")
            
            # 创建目标目录（如果不存在）
            os.makedirs(json_directory, exist_ok=True)
            
            # 将重新格式化的JSON数据写入文件
            write_json_to_file(formatted_json_data, json_file_path)
        
        except ET.ParseError as e:
            print(f"Failed to parse XML file: {aid_file_path} or {simdata_file_path}")
            print(f"Error: {e}")

# 处理AID和SimData目录
process_subdirectory('AID', 'SimData')

print("All data has been processed and written to JSON files.")