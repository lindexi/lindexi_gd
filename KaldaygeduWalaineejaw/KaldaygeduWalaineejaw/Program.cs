using System;
using System.Data.OleDb;

namespace KaldaygeduWalaineejaw
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = @"f:\temp\带密码的课件PPTX.pptx";
            OleDbConnection connection = new OleDbConnection($"Provider=SQLOLEDB;Data Source={file}");
            connection.Open();


        }
    }
}
