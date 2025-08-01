using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 逻辑组织关系：
///
///- Context：对外的上下文定义 `Context_` 文件夹的内容，只包含类型定义
///   - Run
///   - Char
///   - Property
///- 文档管理 <see cref="DocumentManager"/>
///   - 段落管理器 <see cref="ParagraphManager"/>：文档包含多个段落，需要有一个段落管理器
///      - 段落 <see cref="ParagraphData"/>：段落里面有多个行，也有多个字符
///         - 文本行 <see cref="LineLayoutData"/>：由于文本行属于布局的概念，独立与字符的概念
///         - 字符：字符包括文档概念的 <see cref="CharData"/> 和布局概念的 <see cref="CharLayoutData"/> 两个类型
///- Segments：各种偏移量坐标定义，隶属于光标系统，但是和文档关系更加紧密
///- UndoRedo：撤销重做相关
///- Utils：只在文档里使用的辅助工具
/// </summary>
/// 此类型仅仅只是用来记录注释的而已
internal class Readme
{
}
