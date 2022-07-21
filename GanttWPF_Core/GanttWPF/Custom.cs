using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Text;

namespace GanttWPF
{
    public static class Custom
    {
        public static System.Data.DataTable GenerateDataTable<T>(this ObservableCollection<TaskDetails> data)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
            System.Data.DataTable dt = new System.Data.DataTable();
            foreach (PropertyDescriptor prop in properties)
            {
                if (prop.Name == "TaskId" || prop.Name == "TaskName" || prop.Name == "StartDate" || prop.Name == "FinishDate")
                {
                    dt.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                }
            }
            foreach (TaskDetails item in data)
            {
                DataRow row = dt.NewRow();
                foreach (PropertyDescriptor pdt in properties)
                {
                    if (pdt.Name == "TaskId" || pdt.Name == "TaskName" || pdt.Name == "StartDate" || pdt.Name == "FinishDate")
                    {
                        row[pdt.Name] = pdt.GetValue(item) ?? DBNull.Value;
                    }
                }
                dt.Rows.Add(row);
            }
            return dt;
        }
    }
}
