using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class KeyWordTag
    {
        public Guid KeyWordId { get; set; }

        public Guid? SelectedId { get; set; }
        public string SelectedValue { get; set; }

    }
    public class ReportTemplateKeywordOptionDto
    {
        public Guid Id { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        public int Index { get; set; }

        public bool Enable { get; set; }

        public DateTime CreateTime { get; set; }
    }
}
