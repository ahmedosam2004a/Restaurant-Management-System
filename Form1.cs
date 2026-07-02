using System.ComponentModel;
using RestaurantManagementApp.Models;
using RestaurantManagementApp.Services;
using MenuItemModel = RestaurantManagementApp.Models.MenuItem;

namespace RestaurantManagementApp;

public partial class Form1 : Form
{
    private readonly RestaurantRepository _repository = new();
    private RestaurantData? _data;
    private readonly System.Windows.Forms.Timer _autoRefreshTimer = new();
    private int _blockingDialogDepth;

    public Form1()
    {
        InitializeComponent();
        InitializeRuntime();
        Load += Form1_Load;
    }

    private void InitializeRuntime()
    {
        _openSystemButton.Click += (_, _) => OpenFullSystem();
        _openReservationsButton.Click += (_, _) => OpenReservations();
        _openCategoriesButton.Click += (_, _) => OpenCategories();
        _refreshButton.Click += (_, _) => ReloadData();

        components ??= new System.ComponentModel.Container();
        components.Add(_autoRefreshTimer);
        _autoRefreshTimer.Interval = 5000;
        _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;
    }

    private void Form1_Load(object? sender, EventArgs e)
    {
        ReloadData();
        _autoRefreshTimer.Start();
    }

    private void ReloadData(bool updateStatus = true)
    {
        try
        {
            _data = _repository.Load();
            RefreshMetrics();

            if (updateStatus)
            {
                SetStatus("تم تحميل قاعدة البيانات");
            }
        }
        catch (Exception ex)
        {
            if (updateStatus)
            {
                _dbChipLabel.Text = "DB: غير متصلة";
                _dbLabel.Text = "DB: غير متصلة";
                SetStatus($"تعذر تحميل قاعدة البيانات: {ex.Message}");
            }
        }
    }

    private void AutoRefreshTimer_Tick(object? sender, EventArgs e)
    {
        if (_blockingDialogDepth > 0)
        {
            return;
        }

        ReloadData(updateStatus: false);
    }

    private void RefreshMetrics()
    {
        if (_data is null)
        {
            return;
        }

        _categoriesValueLabel.Text = _data.Categories.Count.ToString();
        _menuItemsValueLabel.Text = _data.MenuItems.Count.ToString();
        _tablesValueLabel.Text = _data.Tables.Count.ToString();
        _reservationsValueLabel.Text = _data.Reservations.Count.ToString();
        _openOrdersValueLabel.Text = _data.Orders.Count(order => !order.IsClosed).ToString();
        _revenueValueLabel.Text = $"{_data.Orders.Where(order => order.IsClosed).Sum(order => order.Total):0.00} ج.م";
        _dbChipLabel.Text = "DB: Ready";
        _dbLabel.Text = $"DB: {_repository.StorageDescription}";
    }

    private void OpenFullSystem()
    {
        using var form = new MainForm();
        form.ShowDialog(this);
        ReloadData();
    }

    private void OpenReservations()
    {
        EnsureDataLoaded();
        if (_data is null)
        {
            return;
        }

        _blockingDialogDepth++;
        try
        {
            using var form = new ReservationManagementForm(_data.Reservations, () => SaveData("تم حفظ الحجوزات"));
            form.ShowDialog(this);
            ReloadData();
        }
        finally
        {
            _blockingDialogDepth--;
        }
    }

    private void OpenCategories()
    {
        EnsureDataLoaded();
        if (_data is null)
        {
            return;
        }

        _blockingDialogDepth++;
        try
        {
            var categories = new BindingList<Category>(_data.Categories);
            var menuItems = new BindingList<MenuItemModel>(_data.MenuItems);

            using var form = new CategoryManagementForm(categories, menuItems, () => SaveData("تم حفظ الأصناف"));
            form.ShowDialog(this);
            ReloadData();
        }
        finally
        {
            _blockingDialogDepth--;
        }
    }

    private void SaveData(string statusMessage)
    {
        if (_data is null)
        {
            return;
        }

        try
        {
            _repository.Save(_data);
            RefreshMetrics();
            SetStatus(statusMessage);
        }
        catch (Exception ex)
        {
            SetStatus($"تعذر حفظ البيانات: {ex.Message}");
        }
    }

    private void EnsureDataLoaded()
    {
        if (_data is not null)
        {
            return;
        }

        ReloadData();
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
    }
}
