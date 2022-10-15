using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace GanttWPF
{
    public class ViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModel"/> class.
        /// </summary>
        public ViewModel()
        {
            _taskDetails = this.GetTaskDetails();
        }

        private ObservableCollection<TaskDetails> _taskDetails;

        /// <summary>
        /// Gets or sets the task collection.
        /// </summary>
        /// <value>The task collection.</value>
        public ObservableCollection<TaskDetails> TaskDetails
        {
            get
            {
                return _taskDetails;
            }
            set
            {
                _taskDetails = value;
            }
        }

        /// <summary>
        /// Gets the task details.
        /// </summary>
        /// <returns></returns>
        ObservableCollection<TaskDetails> GetTaskDetails()
        {
            ObservableCollection<TaskDetails> task = new ObservableCollection<TaskDetails>();
            task.Add(new TaskDetails { TaskId = 1, TaskName = "Scope", StartDate = new DateTime(2011, 7, 3), FinishDate = new DateTime(2011, 7, 14), Progress = 40d });
            task.Add(new TaskDetails { TaskId = 2, TaskName = "Risk Assessment", StartDate = new DateTime(2011, 7, 8), FinishDate = new DateTime(2011, 7, 24), Progress = 30d });
            task.Add(new TaskDetails { TaskId = 3, TaskName = "Monitoring", StartDate = new DateTime(2011, 7, 13), FinishDate = new DateTime(2011, 8, 6), Progress = 40d });
            task.Add(new TaskDetails { TaskId = 4, TaskName = "Post Implementation", StartDate = new DateTime(2011, 8, 7), FinishDate = new DateTime(2011, 8, 19), Progress = 40d });
            return task;
        }
    }

    public class TaskDetails
    {
        public int TaskId { get; set; }
        public string TaskName { get; set; }
        public DateTime StartDate { get; set; }

        public DateTime FinishDate { get; set; }

        public double Progress { get; set; }


        public TaskDetails()
        {

        }


    }
}
