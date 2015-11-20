using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Toast_通知
{
    public partial class viewModel: notify_property
    {
        public viewModel()
        {
            //ce();   
           
        }
        /// <summary>
        /// 显示在前台的string
        /// </summary>
        public string text
        {
            set;
            get;
        } = "text";
        /// <summary>
        /// 修改text
        /// </summary>
        public void modifytext()
        {
            text = "修改"; 
        }
        /// <summary>
        /// 显示text
        /// </summary>
        public void revealtext()
        {
            
        }

        public void ce()
        {
            //选择消息模板            
            //ToastImageAndText01 在三行文本中被包装的大型图像和单个字符串          
            //ToastImageAndText02 大图像、加粗文本的一个字符串在第一行、常规文本的一个字符串包装在第二、三行中           
            //ToastImageAndText03 大图像、加粗文本的一个字符串被包装在开头两行中、常规文本的一个字符串包装在第三行中            
            //ToastImageAndText04 大图像、加粗文本的一个字符串在第一行、常规文本的一个字符串在第二行中、常规文本的一个字符串在第三行中          
            //ToastText01 包装在三行文本中的单个字符串             
            //ToastText02 第一行中加粗文本的一个字符串、覆盖第二行和第三行的常规文本的一个字符串。            
            //ToastText03 覆盖第一行和第二行的加粗文本的一个字符串。第三行中常规文本的一个字符串            
            //ToastText04 第一行中加粗文本的一个字符串、第二行中常规文本的一个字符串、第三行中常规文本的一个字符串
            var t = Windows.UI.Notifications.ToastTemplateType.ToastText02;
            //在模板添加xml要的标题
            var content = Windows.UI.Notifications.ToastNotificationManager.GetTemplateContent(t);
            //需要using Windows.Data.Xml.Dom;
            XmlNodeList xml = content.GetElementsByTagName("text");
            xml[0].AppendChild(content.CreateTextNode("通知"));
            xml[1].AppendChild(content.CreateTextNode("小文本"));
            //需要using Windows.UI.Notifications;
            Windows.UI.Notifications.ToastNotification toast = new ToastNotification(content);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
        
    }
}
