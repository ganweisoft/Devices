<p align="left" dir="auto">
  <a href="https://opensource.ganweicloud.com" rel="nofollow">
    <img style="width:130px;height:130px;" src="https://github.com/ganweisoft/Devices/blob/main/src/src/logo.jpg">
  </a>
</p>

[![GitHub license](https://camo.githubusercontent.com/5eaf3ed8a7e8ccb15c21d967b8635ac79e8b1865da3a5ccf78d2572a3e10738a/68747470733a2f2f696d672e736869656c64732e696f2f6769746875622f6c6963656e73652f646f746e65742f6173706e6574636f72653f636f6c6f723d253233306230267374796c653d666c61742d737175617265)](https://github.com/ganweisoft/Devices/blob/main/LICENSE) ![AppVeyor](https://ci.appveyor.com/api/projects/status/v8gfh6pe2u2laqoa?svg=true) ![](https://img.shields.io/badge/join-discord-infomational)

简体中文 | [English](README.md)

Devices 原生支持 Modbus 与 OPC UA（Open Platform Communications Unified Architecture） 两种工业自动化领域主流通信协议，提供高效、可靠的数据采集与设备交互能力。
# 源代码结构说明
```bash
|-- GWModbusStandard.STD          # Modbus协议标准实现模块
|   |-- Core                      # Modbus核心协议实现
|   |-- Helper                    # Modbus协议辅助工具类
|   |-- Model                     # Modbus数据模型定义
|   |-- Service                   # Modbus通信服务实现
|-- GWOpcUAStandard.STD           # OPC UA协议标准实现模块
    |-- Helper                    # OPC UA协议辅助工具类
    |-- Model                     # OPC UA信息模型定义
    |-- Service                   # OPC UA服务端/客户端实现
    |-- lib                       # OPC UA协议栈依赖库
```

### License

Devices 使用非常宽松的MIT协议，请见 [License](https://github.com/ganweisoft/Devices/blob/main/LICENSE)。

### 如何提交贡献

我们非常欢迎开发者提交贡献, 如果您发现了一个bug或者有一些想法想要交流，欢迎提交一个[issue](https://github.com/ganweisoft/Devices/blob/main/CONTRIBUTING.md).
