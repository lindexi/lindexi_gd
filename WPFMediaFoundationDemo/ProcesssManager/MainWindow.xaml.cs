using Lsj.Util.Win32.BaseTypes;
using System.Windows;
using static Lsj.Util.Win32.Kernel32;
using static Lsj.Util.Win32.UnsafePInvokeExtensions;
using static Lsj.Util.Win32.Enums.ProcessAccessRights;
using static Lsj.Util.Win32.Enums.SystemErrorCodes;
using static Lsj.Util.Win32.Constants;
using Lsj.Util.Win32.Structs;

namespace ProcesssManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitList();
        }

        private void InitList()
        {
            int count = 0;
            var processIds = new DWORD[100];
            while (true)
            {
                var size = processIds.Length * SizeOf<DWORD>();
                if (EnumProcesses(processIds, size, out var needed))
                {
                    if (needed >= size)
                    {
                        processIds = new DWORD[processIds.Length * 2];
                    }
                    else
                    {
                        count = needed / SizeOf<DWORD>();
                        break;
                    }
                }
            }

            var result = new List<ProcessItem>(count);
            for (int i = 0; i < count; i++)
            {
                var id = processIds[i];
                var handle = OpenProcess(PROCESS_QUERY_INFORMATION, false, id);
                if (handle == NULL)
                {
                    if (GetLastError() == ERROR_ACCESS_DENIED)
                    {
                        handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, id);
                    }
                    else
                    {

                    }
                }

                if (handle != NULL)
                {
                    result.Add(new ProcessItem(id, handle));
                }
                else
                {
                    continue;
                }
            }

            var totalPageFileUsage = result.Sum(x => x.PageFileUsage) / 1024d / 1024 / 1024;
            var totalWorkingSetSize = result.Sum(x => x.WorkingSetSize) / 1024d / 1024 / 1024;
        }

        public class ProcessItem
        {
            public uint ProcessID { get; }

            public HANDLE Handle { get; }

            public long PageFileUsage { get; }

            public long WorkingSetSize { get; }

            public long PagedPoolUsage { get; }

            public long NonPagedPoolUsage { get; }


            public ProcessItem(uint processId, HANDLE handle)
            {
                ProcessID = processId;
                Handle = handle;

                var counters = new PROCESS_MEMORY_COUNTERS_EX2();
                if (GetProcessMemoryInfo(handle, ref As<PROCESS_MEMORY_COUNTERS_EX2, PROCESS_MEMORY_COUNTERS>(ref counters), SizeOf<PROCESS_MEMORY_COUNTERS_EX2>()))
                {
                    PageFileUsage = counters.PagefileUsage;
                    WorkingSetSize = counters.WorkingSetSize;
                }
            }
        }
    }
}