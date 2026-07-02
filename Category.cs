using System.ComponentModel;

namespace RestaurantManagementApp.Models;

public class Category : INotifyPropertyChanged
{
    private int _id;
    private string _name = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Id
    {
        get => _id;
        set
        {
            if (_id == value)
            {
                return;
            }

            _id = value;
            OnPropertyChanged(nameof(Id));
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.Equals(_name, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _name = normalized;
            OnPropertyChanged(nameof(Name));
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
