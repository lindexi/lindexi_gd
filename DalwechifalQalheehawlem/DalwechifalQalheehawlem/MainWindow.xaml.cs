using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DalwechifalQalheehawlem;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}

public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        RaisePropertyChanged(propertyName);
        return true;
    }
}

public class Student
{
    public int Id { get; set; }
    public string? Name { get; set; }
}
public class MainViewModel : ViewModelBase
{
    private ObservableCollection<Student> _students;
    private Student _selectedStudents;
    public ObservableCollection<Student> Students
    {
        get
        {
            if (_students == null)
            {
                _students = new ObservableCollection<Student>();
                _students.Add(new Student() { Id = 1, Name = "A" });
                _students.Add(new Student() { Id = 2, Name = "B" });
                _students.Add(new Student() { Id = 3, Name = "C" });
            }
            return _students;
        }
    }

    public Student SelectedStudents
    {
        get
        {
            return _selectedStudents;
        }
        set
        {
            _selectedStudents = value;
            RaisePropertyChanged(nameof(SelectedStudents));
        }
    }
}
