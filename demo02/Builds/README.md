# Builds

本目录只存放本地测试构建，JSON 和用户设置不会进入 Git。

当前 Windows 构建：

```text
WindowsDemo/HuaiZuoChronicles.exe
```

发布压缩包：

```text
Releases/HuaiZuoChronicles_Windows_x64_v1.48.0.zip
```

建议命名：

```text
Demo02_YYYY-MM-DD_<milestone>_win64/
Demo02_YYYY-MM-DD_<milestone>_win64.zip
```

需要长期保留并对外交付的版本，确认可以运行、没有敏感文件后，再单独建立发布记录。不要把整个构建目录直接提交到普通 Git。
