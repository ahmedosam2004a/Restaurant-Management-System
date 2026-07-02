using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using RestaurantManagementApp.Models;
using RestaurantManagementApp.Services;
using MenuItemModel = RestaurantManagementApp.Models.MenuItem;

namespace RestaurantManagementApp;

public partial class MainForm : Form
{
    private readonly RestaurantRepository _repository;
    private readonly RestaurantData _data;
    private readonly BindingList<Category> _categories;
    private readonly BindingList<MenuItemModel> _menuItems;
    private readonly BindingList<RestaurantTable> _tables;
    private readonly BindingList<RestaurantOrder> _orders;
    private readonly BindingList<OrderItem> _currentOrderItems = new();

    private readonly BindingSource _categorySource = new();
    private readonly BindingSource _menuSource = new();
    private readonly BindingSource _tableSource = new();
    private readonly BindingSource _orderSource = new();
    private readonly BindingSource _currentOrderItemsSource = new();

    private RestaurantOrder? _currentOrder;

    public MainForm()
    {
        var designMode = LicenseManager.UsageMode == LicenseUsageMode.Designtime;
        _repository = new RestaurantRepository();
        _data = designMode ? CreateDesignTimeData() : _repository.Load();
        _categories = new BindingList<Category>(_data.Categories);
        _menuItems = new BindingList<MenuItemModel>(_data.MenuItems);
        _tables = new BindingList<RestaurantTable>(_data.Tables);
        _orders = new BindingList<RestaurantOrder>(_data.Orders);

        InitializeWindow();
        InitializeComponent();
        WireEvents();
        BindData();
        ConfigureGridColumns();
        RestoreInitialSelection();
        UpdateDashboard();
        UpdateCurrentOrderSummary();
        UpdateOrderEditorState();
        SetStatus(designMode
            ? "وضع التصميم"
            : $"تم تحميل البيانات من {_repository.StorageDescription}");
    }

    private static RestaurantData CreateDesignTimeData()
    {
        return new RestaurantData
        {
            Categories = new List<Category>
            {
                new() { Id = 1, Name = "ساندوتشات" },
                new() { Id = 2, Name = "وجبات" },
                new() { Id = 3, Name = "مشروبات" }
            },
            MenuItems = new List<MenuItemModel>
            {
                new() { Id = 1, Name = "شاورما دجاج", Category = "ساندوتشات", Price = 45m },
                new() { Id = 2, Name = "برجر لحم", Category = "وجبات", Price = 70m },
                new() { Id = 3, Name = "شاي", Category = "مشروبات", Price = 15m }
            },
            Tables = new List<RestaurantTable>
            {
                new() { Id = 1, Name = "طاولة 1", Seats = 2 },
                new() { Id = 2, Name = "طاولة 2", Seats = 4 }
            },
            Orders = new List<RestaurantOrder>(),
            NextCategoryId = 4,
            NextMenuItemId = 4,
            NextTableId = 3,
            NextOrderId = 1
        };
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        TrySaveData("تم حفظ آخر التغييرات");
        base.OnFormClosing(e);
    }

    private void InitializeWindow()
    {
        Text = "نظام إدارة المطعم";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1420, 900);
        MinimumSize = new Size(1220, 780);
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Font;
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = BackColor
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

        root.Controls.Add(CreateHeaderPanel(), 0, 0);
        root.Controls.Add(CreateTabs(), 0, 1);
        root.Controls.Add(CreateStatusStrip(), 0, 2);

        Controls.Add(root);
    }

    private Panel CreateHeaderPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(15, 23, 42),
            Padding = new Padding(18, 12, 18, 12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = panel.BackColor
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var titleLabel = new Label
        {
            Text = "نظام إدارة المطعم",
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font(Font.FontFamily, 19F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var subtitleLabel = new Label
        {
            Text = "إدارة الأصناف والطاولات والطلبات والحسابات من شاشة واحدة",
            Dock = DockStyle.Fill,
            ForeColor = Color.Gainsboro,
            Font = new Font(Font.FontFamily, 10F, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _manageReservationsButton = CreateButton("فورم الحجز", Color.FromArgb(59, 130, 246));

        var actionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = false,
            BackColor = panel.BackColor,
            Margin = new Padding(0)
        };
        actionsPanel.Controls.Add(_manageReservationsButton);

        layout.Controls.Add(titleLabel, 0, 0);
        layout.Controls.Add(actionsPanel, 1, 0);
        layout.SetRowSpan(actionsPanel, 1);
        layout.Controls.Add(subtitleLabel, 0, 1);
        layout.SetColumnSpan(subtitleLabel, 2);
        panel.Controls.Add(layout);
        return panel;
    }

    private StatusStrip CreateStatusStrip()
    {
        var strip = new StatusStrip
        {
            SizingGrip = false,
            BackColor = Color.White
        };

        _statusLabel = new ToolStripStatusLabel
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(55, 65, 81),
            Text = "جاهز"
        };

        _fileLabel = new ToolStripStatusLabel
        {
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.FromArgb(55, 65, 81),
            Text = $"القاعدة: {_repository.StorageDescription}"
        };

        strip.Items.Add(_statusLabel);
        strip.Items.Add(_fileLabel);
        return strip;
    }

    private TabControl CreateTabs()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Appearance = TabAppearance.Normal,
            Padding = new Point(16, 6)
        };

        tabs.TabPages.Add(CreateMenuTab());
        tabs.TabPages.Add(CreateTablesTab());
        tabs.TabPages.Add(CreateOrdersTab());
        tabs.TabPages.Add(CreateReportsTab());

        return tabs;
    }

    private TabPage CreateMenuTab()
    {
        var page = new TabPage("الأصناف")
        {
            BackColor = Color.WhiteSmoke,
            Padding = new Padding(12)
        };

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterDistance = 380,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = page.BackColor
        };
        split.Panel1.Padding = new Padding(0, 0, 12, 0);

        split.Panel1.Controls.Add(CreateMenuEditorGroup());
        split.Panel2.Controls.Add(CreateGridGroup("قائمة الأصناف", _menuGrid));

        page.Controls.Add(split);
        return page;
    }

    private TabPage CreateTablesTab()
    {
        var page = new TabPage("الطاولات")
        {
            BackColor = Color.WhiteSmoke,
            Padding = new Padding(12)
        };

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterDistance = 380,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = page.BackColor
        };
        split.Panel1.Padding = new Padding(0, 0, 12, 0);

        split.Panel1.Controls.Add(CreateTableEditorGroup());
        split.Panel2.Controls.Add(CreateGridGroup("الطاولات الحالية", _tableGrid));

        page.Controls.Add(split);
        return page;
    }

    private TabPage CreateOrdersTab()
    {
        var page = new TabPage("الطلبات")
        {
            BackColor = Color.WhiteSmoke,
            Padding = new Padding(12)
        };

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterDistance = 390,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = page.BackColor
        };
        split.Panel1.Padding = new Padding(0, 0, 12, 0);

        split.Panel1.Controls.Add(CreateOrderEditorGroup());
        split.Panel2.Controls.Add(CreateOrderDetailsPanel());

        page.Controls.Add(split);
        return page;
    }

    private TabPage CreateReportsTab()
    {
        var page = new TabPage("التقارير")
        {
            BackColor = Color.WhiteSmoke,
            Padding = new Padding(12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 122));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(CreateSummaryCardsPanel(), 0, 0);
        layout.Controls.Add(CreateGridGroup("سجل الطلبات", _historyGrid), 0, 1);

        page.Controls.Add(layout);
        return page;
    }

    private GroupBox CreateMenuEditorGroup()
    {
        var group = new GroupBox
        {
            Text = "بيانات الصنف",
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        var layout = CreateInputTable(5);

        _menuNameTextBox = new TextBox();
        _menuCategoryCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FormattingEnabled = true
        };
        _menuPriceUpDown = new NumericUpDown
        {
            DecimalPlaces = 2,
            Minimum = 0.01m,
            Maximum = 1000000m,
            Value = 1m,
            ThousandsSeparator = true,
            TextAlign = HorizontalAlignment.Right
        };

        AddLabeledRow(layout, 0, "اسم الصنف", _menuNameTextBox);
        AddLabeledRow(layout, 1, "التصنيف", _menuCategoryCombo);
        AddLabeledRow(layout, 2, "السعر", _menuPriceUpDown);

        _addMenuButton = CreateButton("إضافة", Color.FromArgb(46, 204, 113));
        _updateMenuButton = CreateButton("تحديث", Color.FromArgb(52, 152, 219));
        _deleteMenuButton = CreateButton("حذف", Color.FromArgb(231, 76, 60));
        _clearMenuButton = CreateButton("تفريغ", Color.FromArgb(149, 165, 166));
        _manageCategoriesButton = CreateButton("إدارة الأنواع", Color.FromArgb(100, 116, 139));

        AddFullRow(layout, 3, CreateButtonRow(_addMenuButton, _updateMenuButton, _deleteMenuButton, _clearMenuButton), 88);
        AddFullRow(layout, 4, _manageCategoriesButton, 44);

        group.Controls.Add(layout);
        return group;
    }

    private GroupBox CreateTableEditorGroup()
    {
        var group = new GroupBox
        {
            Text = "بيانات الطاولة",
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        var layout = CreateInputTable(4);

        _tableNameTextBox = new TextBox();
        _tableSeatsUpDown = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 20,
            Value = 2,
            TextAlign = HorizontalAlignment.Right
        };

        AddLabeledRow(layout, 0, "اسم الطاولة", _tableNameTextBox);
        AddLabeledRow(layout, 1, "عدد المقاعد", _tableSeatsUpDown);

        _addTableButton = CreateButton("إضافة", Color.FromArgb(46, 204, 113));
        _updateTableButton = CreateButton("تحديث", Color.FromArgb(52, 152, 219));
        _deleteTableButton = CreateButton("حذف", Color.FromArgb(231, 76, 60));
        _freeTableButton = CreateButton("تفريغ", Color.FromArgb(243, 156, 18));

        AddFullRow(layout, 2, CreateButtonRow(_addTableButton, _updateTableButton, _deleteTableButton, _freeTableButton), 88);

        group.Controls.Add(layout);
        return group;
    }

    private GroupBox CreateOrderEditorGroup()
    {
        var group = new GroupBox
        {
            Text = "التحكم في الطلب",
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        var layout = CreateInputTable(5);

        _orderTableCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill
        };

        _orderItemCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill
        };

        _quantityUpDown = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 100,
            Value = 1,
            TextAlign = HorizontalAlignment.Right
        };

        _openOrderButton = CreateButton("فتح / تحميل", Color.FromArgb(41, 128, 185));
        _addOrderItemButton = CreateButton("إضافة صنف", Color.FromArgb(46, 204, 113));
        _removeOrderItemButton = CreateButton("حذف صنف", Color.FromArgb(231, 76, 60));
        _closeOrderButton = CreateButton("إغلاق الفاتورة", Color.FromArgb(143, 68, 173));

        AddLabeledRow(layout, 0, "الطاولة", _orderTableCombo);
        AddFullRow(layout, 1, _openOrderButton, 40);
        AddLabeledRow(layout, 2, "الصنف", _orderItemCombo);
        AddLabeledRow(layout, 3, "الكمية", _quantityUpDown);
        AddFullRow(layout, 4, CreateButtonRow(_addOrderItemButton, _removeOrderItemButton, _closeOrderButton), 88);

        group.Controls.Add(layout);
        return group;
    }

    private Panel CreateOrderDetailsPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));

        panel.Controls.Add(CreateGridGroup("بنود الطلب الحالي", _currentOrderGrid), 0, 0);

        var summaryPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            BackColor = Color.White
        };

        var summaryLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1
        };
        summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27));
        summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        _currentOrderInfoLabel = CreateInfoLabel("لا يوجد طلب مفتوح");
        _currentOrderItemsLabel = CreateInfoLabel("عدد الأصناف: 0");
        _currentOrderTotalLabel = CreateInfoLabel("الإجمالي: 0.00 ج.م");

        summaryLayout.Controls.Add(CreateMiniSummaryPanel("الطلب الحالي", _currentOrderInfoLabel, Color.FromArgb(41, 128, 185)), 0, 0);
        summaryLayout.Controls.Add(CreateMiniSummaryPanel("عدد الأصناف", _currentOrderItemsLabel, Color.FromArgb(46, 204, 113)), 1, 0);
        summaryLayout.Controls.Add(CreateMiniSummaryPanel("الإجمالي", _currentOrderTotalLabel, Color.FromArgb(143, 68, 173)), 2, 0);

        summaryPanel.Controls.Add(summaryLayout);
        panel.Controls.Add(summaryPanel, 0, 1);

        return panel;
    }

    private Panel CreateSummaryCardsPanel()
    {
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0),
            WrapContents = true,
            AutoScroll = false,
            BackColor = Color.Transparent
        };

        flow.Controls.Add(CreateSummaryCard("عدد الأصناف", out _menuCountValue, Color.FromArgb(52, 152, 219)));
        flow.Controls.Add(CreateSummaryCard("عدد الطاولات", out _tableCountValue, Color.FromArgb(46, 204, 113)));
        flow.Controls.Add(CreateSummaryCard("الطلبات المفتوحة", out _openOrdersValue, Color.FromArgb(243, 156, 18)));
        flow.Controls.Add(CreateSummaryCard("عدد الحجوزات", out _reservationCountValue, Color.FromArgb(59, 130, 246)));
        flow.Controls.Add(CreateSummaryCard("إجمالي الإيراد", out _revenueValue, Color.FromArgb(143, 68, 173)));

        return flow;
    }

    private static Control CreateGridGroup(string title, DataGridView grid)
    {
        var group = new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        grid.Dock = DockStyle.Fill;
        group.Controls.Add(grid);
        return group;
    }

    private static Control CreateMiniSummaryPanel(string caption, Control value, Color accentColor)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(10)
        };

        var accent = new Panel
        {
            Dock = DockStyle.Left,
            Width = 6,
            BackColor = accentColor
        };

        var captionLabel = new Label
        {
            Text = caption,
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Color.FromArgb(75, 85, 99),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        value.Dock = DockStyle.Fill;

        panel.Controls.Add(value);
        panel.Controls.Add(captionLabel);
        panel.Controls.Add(accent);
        return panel;
    }

    private Control CreateSummaryCard(string caption, out Label valueLabel, Color accentColor)
    {
        var card = new Panel
        {
            Width = 245,
            Height = 88,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 12, 12),
            Padding = new Padding(10)
        };

        var accent = new Panel
        {
            Dock = DockStyle.Left,
            Width = 6,
            BackColor = accentColor
        };

        var captionLabel = new Label
        {
            Text = caption,
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Color.FromArgb(75, 85, 99),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        valueLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "0",
            ForeColor = Color.FromArgb(17, 24, 39),
            Font = new Font("Segoe UI", 20F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        card.Controls.Add(valueLabel);
        card.Controls.Add(captionLabel);
        card.Controls.Add(accent);
        return card;
    }

    private static TableLayoutPanel CreateInputTable(int rows)
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = rows
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        return table;
    }

    private static void AddLabeledRow(TableLayoutPanel table, int rowIndex, string labelText, Control control)
    {
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        var label = new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(55, 65, 81)
        };

        control.Dock = DockStyle.Fill;

        table.Controls.Add(label, 0, rowIndex);
        table.Controls.Add(control, 1, rowIndex);
    }

    private static void AddFullRow(TableLayoutPanel table, int rowIndex, Control control, int height)
    {
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        control.Dock = DockStyle.Fill;
        table.Controls.Add(control, 0, rowIndex);
        table.SetColumnSpan(control, 2);
    }

    private static FlowLayoutPanel CreateButtonRow(params Button[] buttons)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        foreach (var button in buttons)
        {
            button.Margin = new Padding(0, 0, 8, 8);
            panel.Controls.Add(button);
        }

        return panel;
    }

    private static Button CreateButton(string text, Color backColor)
    {
        return new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 6, 12, 6),
            MinimumSize = new Size(84, 34),
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand,
            Margin = new Padding(0)
        };
    }

    private static Label CreateInfoLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(55, 65, 81),
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static DataGridView CreateGrid()
    {
        return new DataGridView
        {
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            AutoGenerateColumns = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 36,
            GridColor = Color.FromArgb(229, 231, 235),
            DefaultCellStyle = new DataGridViewCellStyle
            {
                SelectionBackColor = Color.FromArgb(219, 234, 254),
                SelectionForeColor = Color.Black
            },
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(31, 41, 55),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 250, 252)
            }
        };
    }

    private void WireEvents()
    {
        _menuGrid.SelectionChanged += (_, _) => PopulateMenuEditorFromSelection();
        _tableGrid.SelectionChanged += (_, _) => PopulateTableEditorFromSelection();
        _currentOrderGrid.SelectionChanged += (_, _) => UpdateOrderEditorState();

        _menuGrid.DataBindingComplete += (_, _) => ConfigureMenuGridColumns();
        _tableGrid.DataBindingComplete += (_, _) => ConfigureTableGridColumns();
        _currentOrderGrid.DataBindingComplete += (_, _) => ConfigureCurrentOrderGridColumns();
        _historyGrid.DataBindingComplete += (_, _) => ConfigureHistoryGridColumns();

        _addMenuButton.Click += (_, _) => AddMenuItem();
        _updateMenuButton.Click += (_, _) => UpdateMenuItem();
        _deleteMenuButton.Click += (_, _) => DeleteMenuItem();
        _clearMenuButton.Click += (_, _) => ClearMenuEditor();
        _manageCategoriesButton.Click += (_, _) => OpenCategoryManagement();
        _manageReservationsButton.Click += (_, _) => OpenReservationManagement();

        _addTableButton.Click += (_, _) => AddTable();
        _updateTableButton.Click += (_, _) => UpdateTable();
        _deleteTableButton.Click += (_, _) => DeleteTable();
        _freeTableButton.Click += (_, _) => FreeTable();

        _openOrderButton.Click += (_, _) => OpenSelectedTableOrder();
        _addOrderItemButton.Click += (_, _) => AddOrderItem();
        _removeOrderItemButton.Click += (_, _) => RemoveSelectedOrderItem();
        _closeOrderButton.Click += (_, _) => CloseCurrentOrder();

        _orderTableCombo.SelectedIndexChanged += (_, _) => UpdateOrderEditorState();
        _orderItemCombo.SelectedIndexChanged += (_, _) => UpdateOrderEditorState();
    }

    private void BindData()
    {
        _categorySource.DataSource = _categories;
        _menuSource.DataSource = _menuItems;
        _tableSource.DataSource = _tables;
        _orderSource.DataSource = _orders;
        _currentOrderItemsSource.DataSource = _currentOrderItems;

        _menuCategoryCombo.DataSource = _categorySource;
        _menuCategoryCombo.DisplayMember = nameof(Category.Name);
        _menuGrid.DataSource = _menuSource;
        _tableGrid.DataSource = _tableSource;
        _currentOrderGrid.DataSource = _currentOrderItemsSource;
        _historyGrid.DataSource = _orderSource;

        _orderTableCombo.DataSource = _tableSource;
        _orderTableCombo.DisplayMember = nameof(RestaurantTable.Name);

        _orderItemCombo.DataSource = _menuSource;
        _orderItemCombo.DisplayMember = nameof(MenuItemModel.Name);
    }

    private void ConfigureGridColumns()
    {
        ConfigureMenuGridColumns();
        ConfigureTableGridColumns();
        ConfigureCurrentOrderGridColumns();
        ConfigureHistoryGridColumns();
    }

    private void ConfigureMenuGridColumns()
    {
        ConfigureColumn(_menuGrid, nameof(MenuItemModel.Id), "الرقم");
        ConfigureColumn(_menuGrid, nameof(MenuItemModel.Name), "اسم الصنف");
        ConfigureColumn(_menuGrid, nameof(MenuItemModel.Category), "التصنيف");
        ConfigureColumn(_menuGrid, nameof(MenuItemModel.Price), "السعر", "0.00");
    }

    private void ConfigureTableGridColumns()
    {
        ConfigureColumn(_tableGrid, nameof(RestaurantTable.Id), "الرقم");
        ConfigureColumn(_tableGrid, nameof(RestaurantTable.Name), "اسم الطاولة");
        ConfigureColumn(_tableGrid, nameof(RestaurantTable.Seats), "المقاعد");
        ConfigureColumn(_tableGrid, nameof(RestaurantTable.Status), "الحالة");
        ConfigureColumn(_tableGrid, nameof(RestaurantTable.IsOccupied), "", null, false);
        ConfigureColumn(_tableGrid, nameof(RestaurantTable.ActiveOrderId), "", null, false);
    }

    private void ConfigureCurrentOrderGridColumns()
    {
        ConfigureColumn(_currentOrderGrid, nameof(OrderItem.MenuItemName), "الصنف");
        ConfigureColumn(_currentOrderGrid, nameof(OrderItem.UnitPrice), "سعر الوحدة", "0.00");
        ConfigureColumn(_currentOrderGrid, nameof(OrderItem.Quantity), "الكمية");
        ConfigureColumn(_currentOrderGrid, nameof(OrderItem.LineTotal), "الإجمالي", "0.00");
        ConfigureColumn(_currentOrderGrid, nameof(OrderItem.MenuItemId), "", null, false);
    }

    private void ConfigureHistoryGridColumns()
    {
        ConfigureColumn(_historyGrid, nameof(RestaurantOrder.Id), "الرقم");
        ConfigureColumn(_historyGrid, nameof(RestaurantOrder.TableName), "الطاولة");
        ConfigureColumn(_historyGrid, nameof(RestaurantOrder.CreatedAt), "تاريخ الإنشاء", "yyyy-MM-dd HH:mm");
        ConfigureColumn(_historyGrid, nameof(RestaurantOrder.ClosedAt), "تاريخ الإغلاق", "yyyy-MM-dd HH:mm");
        ConfigureColumn(_historyGrid, nameof(RestaurantOrder.Status), "الحالة");
        ConfigureColumn(_historyGrid, nameof(RestaurantOrder.ItemsCount), "عدد الأصناف");
        ConfigureColumn(_historyGrid, nameof(RestaurantOrder.Total), "الإجمالي", "0.00");
        ConfigureColumn(_historyGrid, nameof(RestaurantOrder.TableId), "", null, false);
        ConfigureColumn(_historyGrid, nameof(RestaurantOrder.Items), "", null, false);
    }

    private static void ConfigureColumn(DataGridView grid, string columnName, string headerText, string? format = null, bool visible = true)
    {
        if (!grid.Columns.Contains(columnName))
        {
            return;
        }

        var column = grid.Columns[columnName];
        column.HeaderText = headerText;
        column.Visible = visible;

        if (!string.IsNullOrWhiteSpace(format))
        {
            column.DefaultCellStyle.Format = format;
        }
    }

    private void RestoreInitialSelection()
    {
        if (_tables.Count > 0)
        {
            _orderTableCombo.SelectedIndex = 0;
        }

        if (_menuItems.Count > 0)
        {
            _orderItemCombo.SelectedIndex = 0;
        }

        if (_categories.Count > 0)
        {
            _menuCategoryCombo.SelectedIndex = 0;
        }

        var openOrder = _orders.FirstOrDefault(order => !order.IsClosed);
        if (openOrder is not null)
        {
            var table = _tables.FirstOrDefault(item => item.Id == openOrder.TableId);
            if (table is not null)
            {
                _orderTableCombo.SelectedItem = table;
                LoadOrderIntoEditor(openOrder);
            }
        }
    }

    private void PopulateMenuEditorFromSelection()
    {
        if (_menuGrid.CurrentRow?.DataBoundItem is not MenuItemModel item)
        {
            return;
        }

        _menuNameTextBox.Text = item.Name;
        var category = _categories.FirstOrDefault(categoryItem => string.Equals(categoryItem.Name, item.Category, StringComparison.OrdinalIgnoreCase));
        _menuCategoryCombo.SelectedItem = category;
        if (category is null && _categories.Count > 0)
        {
            _menuCategoryCombo.SelectedIndex = 0;
        }
        _menuPriceUpDown.Value = Math.Min(Math.Max(item.Price, _menuPriceUpDown.Minimum), _menuPriceUpDown.Maximum);
    }

    private void PopulateTableEditorFromSelection()
    {
        if (_tableGrid.CurrentRow?.DataBoundItem is not RestaurantTable table)
        {
            return;
        }

        _tableNameTextBox.Text = table.Name;
        _tableSeatsUpDown.Value = Math.Min(Math.Max(table.Seats, (int)_tableSeatsUpDown.Minimum), (int)_tableSeatsUpDown.Maximum);
    }

    private void AddMenuItem()
    {
        var name = _menuNameTextBox.Text.Trim();
        var category = _menuCategoryCombo.SelectedItem as Category;
        var price = _menuPriceUpDown.Value;

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowWarning("اكتب اسم الصنف أولًا.");
            return;
        }

        if (category is null)
        {
            ShowWarning("اختر نوع الصنف أولًا.");
            return;
        }

        var item = new MenuItemModel
        {
            Id = _data.NextMenuItemId++,
            Name = name,
            Category = category.Name,
            Price = price
        };

        _menuItems.Add(item);
        _menuSource.ResetBindings(false);
        _orderSource.ResetBindings(false);
        SelectMenuItem(item);
        ClearMenuEditorFields();
        CommitChanges("تمت إضافة الصنف");
    }

    private void UpdateMenuItem()
    {
        if (_menuGrid.CurrentRow?.DataBoundItem is not MenuItemModel item)
        {
            ShowWarning("اختر صنفًا من الجدول أولًا.");
            return;
        }

        var name = _menuNameTextBox.Text.Trim();
        var category = _menuCategoryCombo.SelectedItem as Category;

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowWarning("اكتب اسم الصنف أولًا.");
            return;
        }

        if (category is null)
        {
            ShowWarning("اختر نوع الصنف أولًا.");
            return;
        }

        item.Name = name;
        item.Category = category.Name;
        item.Price = _menuPriceUpDown.Value;

        _menuSource.ResetBindings(false);
        _orderSource.ResetBindings(false);
        _currentOrderItemsSource.ResetBindings(false);
        CommitChanges("تم تحديث الصنف");
    }

    private void DeleteMenuItem()
    {
        if (_menuGrid.CurrentRow?.DataBoundItem is not MenuItemModel item)
        {
            ShowWarning("اختر صنفًا من الجدول أولًا.");
            return;
        }

        if (MessageBox.Show(this, $"هل تريد حذف الصنف '{item.Name}'؟", "تأكيد الحذف",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        _menuItems.Remove(item);
        _menuSource.ResetBindings(false);
        _orderSource.ResetBindings(false);
        ClearMenuEditorFields();
        CommitChanges("تم حذف الصنف");
    }

    private void ClearMenuEditor()
    {
        ClearMenuEditorFields();
        _menuGrid.ClearSelection();
        _menuGrid.CurrentCell = null;
    }

    private void ClearMenuEditorFields()
    {
        _menuNameTextBox.Clear();
        if (_categories.Count > 0)
        {
            _menuCategoryCombo.SelectedIndex = 0;
        }
        else
        {
            _menuCategoryCombo.SelectedIndex = -1;
        }
        _menuPriceUpDown.Value = 1m;
    }

    private void AddTable()
    {
        var name = _tableNameTextBox.Text.Trim();
        var seats = (int)_tableSeatsUpDown.Value;

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowWarning("اكتب اسم الطاولة أولًا.");
            return;
        }

        var table = new RestaurantTable
        {
            Id = _data.NextTableId++,
            Name = name,
            Seats = seats,
            IsOccupied = false,
            ActiveOrderId = null
        };

        _tables.Add(table);
        _tableSource.ResetBindings(false);
        SelectTable(table);
        ClearTableEditorFields();
        CommitChanges("تمت إضافة الطاولة");
    }

    private void UpdateTable()
    {
        if (_tableGrid.CurrentRow?.DataBoundItem is not RestaurantTable table)
        {
            ShowWarning("اختر طاولة من الجدول أولًا.");
            return;
        }

        var name = _tableNameTextBox.Text.Trim();
        var seats = (int)_tableSeatsUpDown.Value;

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowWarning("اكتب اسم الطاولة أولًا.");
            return;
        }

        table.Name = name;
        table.Seats = seats;

        var activeOrder = _orders.FirstOrDefault(order => order.Id == table.ActiveOrderId && !order.IsClosed);
        if (activeOrder is not null)
        {
            activeOrder.TableName = table.Name;
            if (_currentOrder?.Id == activeOrder.Id)
            {
                _currentOrderInfoLabel.Text = $"الطلب: #{_currentOrder.Id} | الطاولة: {table.Name}";
            }
        }

        _tableSource.ResetBindings(false);
        _orderSource.ResetBindings(false);
        CommitChanges("تم تحديث الطاولة");
    }

    private void DeleteTable()
    {
        if (_tableGrid.CurrentRow?.DataBoundItem is not RestaurantTable table)
        {
            ShowWarning("اختر طاولة من الجدول أولًا.");
            return;
        }

        if (table.IsOccupied)
        {
            ShowWarning("لا يمكن حذف طاولة مشغولة. أغلق الطلب أولًا.");
            return;
        }

        if (MessageBox.Show(this, $"هل تريد حذف الطاولة '{table.Name}'؟", "تأكيد الحذف",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        _tables.Remove(table);
        _tableSource.ResetBindings(false);
        ClearTableEditorFields();
        CommitChanges("تم حذف الطاولة");
    }

    private void FreeTable()
    {
        if (_tableGrid.CurrentRow?.DataBoundItem is not RestaurantTable table)
        {
            ShowWarning("اختر طاولة من الجدول أولًا.");
            return;
        }

        var activeOrder = table.ActiveOrderId is null
            ? null
            : _orders.FirstOrDefault(order => order.Id == table.ActiveOrderId.Value && !order.IsClosed);

        if (activeOrder is not null)
        {
            ShowWarning("أغلق الطلب المرتبط بهذه الطاولة أولًا.");
            return;
        }

        table.IsOccupied = false;
        table.ActiveOrderId = null;
        _tableSource.ResetBindings(false);
        CommitChanges("تم تفريغ الطاولة");
    }

    private void ClearTableEditorFields()
    {
        _tableNameTextBox.Clear();
        _tableSeatsUpDown.Value = 2;
    }

    private void OpenSelectedTableOrder()
    {
        var table = _orderTableCombo.SelectedItem as RestaurantTable;
        if (table is null)
        {
            ShowWarning("اختر طاولة أولًا.");
            return;
        }

        SyncCurrentOrderToModel();

        var openOrder = _orders.FirstOrDefault(order => order.TableId == table.Id && !order.IsClosed);
        if (openOrder is null)
        {
            openOrder = new RestaurantOrder
            {
                Id = _data.NextOrderId++,
                TableId = table.Id,
                TableName = table.Name,
                CreatedAt = DateTime.Now,
                ClosedAt = null,
                Items = new List<OrderItem>()
            };

            _orders.Add(openOrder);
        }
        else
        {
            openOrder.TableName = table.Name;
        }

        table.IsOccupied = true;
        table.ActiveOrderId = openOrder.Id;

        LoadOrderIntoEditor(openOrder);
        _tableSource.ResetBindings(false);
        _orderSource.ResetBindings(false);
        UpdateDashboard();
        UpdateOrderEditorState();
        CommitChanges("تم فتح الطلب");
    }

    private void AddOrderItem()
    {
        if (_currentOrder is null)
        {
            ShowWarning("افتح طلب الطاولة أولًا.");
            return;
        }

        var menuItem = _orderItemCombo.SelectedItem as MenuItemModel;
        if (menuItem is null)
        {
            ShowWarning("اختر صنفًا من قائمة الأصناف.");
            return;
        }

        var quantity = (int)_quantityUpDown.Value;
        if (quantity <= 0)
        {
            ShowWarning("اكتب كمية صحيحة.");
            return;
        }

        var existing = _currentOrderItems.FirstOrDefault(item => item.MenuItemId == menuItem.Id);
        if (existing is not null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            _currentOrderItems.Add(new OrderItem
            {
                MenuItemId = menuItem.Id,
                MenuItemName = menuItem.Name,
                UnitPrice = menuItem.Price,
                Quantity = quantity
            });
        }

        SyncCurrentOrderToModel();
        _currentOrderItemsSource.ResetBindings(false);
        _orderSource.ResetBindings(false);
        UpdateCurrentOrderSummary();
        UpdateDashboard();
        UpdateOrderEditorState();
        CommitChanges("تمت إضافة الصنف إلى الطلب");
    }

    private void RemoveSelectedOrderItem()
    {
        if (_currentOrder is null)
        {
            ShowWarning("لا يوجد طلب مفتوح.");
            return;
        }

        var selectedItem = _currentOrderGrid.CurrentRow?.DataBoundItem as OrderItem;
        if (selectedItem is null)
        {
            ShowWarning("اختر صنفًا من الطلب أولًا.");
            return;
        }

        _currentOrderItems.Remove(selectedItem);
        SyncCurrentOrderToModel();
        _currentOrderItemsSource.ResetBindings(false);
        _orderSource.ResetBindings(false);
        UpdateCurrentOrderSummary();
        UpdateDashboard();
        UpdateOrderEditorState();
        CommitChanges("تم حذف الصنف من الطلب");
    }

    private void CloseCurrentOrder()
    {
        if (_currentOrder is null)
        {
            ShowWarning("لا يوجد طلب مفتوح.");
            return;
        }

        SyncCurrentOrderToModel();
        _currentOrder.ClosedAt = DateTime.Now;

        var table = _tables.FirstOrDefault(item => item.Id == _currentOrder.TableId);
        if (table is not null)
        {
            table.IsOccupied = false;
            table.ActiveOrderId = null;
        }

        _currentOrder = null;
        _currentOrderItems.Clear();
        _currentOrderItemsSource.ResetBindings(false);
        _orderSource.ResetBindings(false);
        _tableSource.ResetBindings(false);

        UpdateCurrentOrderSummary();
        UpdateDashboard();
        UpdateOrderEditorState();
        CommitChanges("تم إغلاق الفاتورة");
    }

    private void LoadOrderIntoEditor(RestaurantOrder order)
    {
        _currentOrder = order;
        _currentOrderItems.Clear();

        foreach (var item in order.Items)
        {
            _currentOrderItems.Add(CloneOrderItem(item));
        }

        if (_orderTableCombo.SelectedItem is RestaurantTable table)
        {
            order.TableName = table.Name;
        }

        _currentOrderItemsSource.ResetBindings(false);
        _orderSource.ResetBindings(false);
        UpdateCurrentOrderSummary();
        UpdateOrderEditorState();
    }

    private static OrderItem CloneOrderItem(OrderItem item)
    {
        return new OrderItem
        {
            MenuItemId = item.MenuItemId,
            MenuItemName = item.MenuItemName,
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity
        };
    }

    private void SyncCurrentOrderToModel()
    {
        if (_currentOrder is null)
        {
            return;
        }

        _currentOrder.Items = _currentOrderItems.Select(CloneOrderItem).ToList();

        var table = _tables.FirstOrDefault(item => item.Id == _currentOrder.TableId);
        if (table is not null)
        {
            _currentOrder.TableName = table.Name;
        }
    }

    private void UpdateCurrentOrderSummary()
    {
        if (_currentOrder is null)
        {
            _currentOrderInfoLabel.Text = "لا يوجد طلب مفتوح";
            _currentOrderItemsLabel.Text = "عدد الأصناف: 0";
            _currentOrderTotalLabel.Text = "الإجمالي: 0.00 ج.م";
            return;
        }

        var total = _currentOrderItems.Sum(item => item.LineTotal);
        _currentOrderInfoLabel.Text = $"الطلب: #{_currentOrder.Id} | الطاولة: {_currentOrder.TableName}";
        _currentOrderItemsLabel.Text = $"عدد الأصناف: {_currentOrderItems.Count}";
        _currentOrderTotalLabel.Text = $"الإجمالي: {total:0.00} ج.م";
    }

    private void UpdateOrderEditorState()
    {
        var hasTable = _orderTableCombo.SelectedItem is RestaurantTable;
        var hasOpenOrder = _currentOrder is not null;
        var hasCurrentSelection = _currentOrderGrid.CurrentRow?.DataBoundItem is OrderItem;
        var hasItems = _currentOrderItems.Count > 0;
        var hasMenu = _menuItems.Count > 0;

        _openOrderButton.Enabled = hasTable;
        _addOrderItemButton.Enabled = hasOpenOrder && hasMenu;
        _removeOrderItemButton.Enabled = hasOpenOrder && (hasCurrentSelection || hasItems);
        _closeOrderButton.Enabled = hasOpenOrder;
    }

    private void UpdateDashboard()
    {
        _menuCountValue.Text = _menuItems.Count.ToString();
        _tableCountValue.Text = _tables.Count.ToString();
        _openOrdersValue.Text = _orders.Count(order => !order.IsClosed).ToString();
        _reservationCountValue.Text = _data.Reservations.Count.ToString();
        _revenueValue.Text = $"{_orders.Where(order => order.IsClosed).Sum(order => order.Total):0.00} ج.م";
    }

    private void RefreshBindingSources()
    {
        _categorySource.ResetBindings(false);
        _menuSource.ResetBindings(false);
        _tableSource.ResetBindings(false);
        _orderSource.ResetBindings(false);
        _currentOrderItemsSource.ResetBindings(false);

        if (_categories.Count > 0 && _menuCategoryCombo.SelectedIndex < 0)
        {
            _menuCategoryCombo.SelectedIndex = 0;
        }
    }

    private void CommitChanges(string statusMessage)
    {
        SyncCurrentOrderToModel();

        try
        {
            _repository.Save(_data);
            RefreshBindingSources();
            UpdateDashboard();
            UpdateCurrentOrderSummary();
            UpdateOrderEditorState();
            SetStatus(statusMessage);
        }
        catch (Exception ex)
        {
            ShowError($"تعذر حفظ البيانات.\n{ex.Message}");
        }
    }

    private void TrySaveData(string statusMessage)
    {
        SyncCurrentOrderToModel();

        try
        {
            _repository.Save(_data);
            SetStatus(statusMessage);
        }
        catch
        {
            // Best effort on exit.
        }
    }

    private void OpenCategoryManagement()
    {
        using var form = new CategoryManagementForm(
            _categories,
            _menuItems,
            () => CommitChanges("تم حفظ الأنواع"));

        form.ShowDialog(this);
        RefreshBindingSources();
        UpdateDashboard();
        UpdateOrderEditorState();

        if (_menuGrid.CurrentRow?.DataBoundItem is MenuItemModel)
        {
            PopulateMenuEditorFromSelection();
        }
    }

    private void OpenReservationManagement()
    {
        using var form = new ReservationManagementForm(
            _data.Reservations,
            () => CommitChanges("تم حفظ الحجوزات"));

        form.ShowDialog(this);
        UpdateDashboard();
        UpdateOrderEditorState();
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
        _fileLabel.Text = $"القاعدة: {_repository.StorageDescription}";
    }

    private static void ShowWarning(string message)
    {
        MessageBox.Show(message, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ShowError(string message)
    {
        MessageBox.Show(this, message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void SelectMenuItem(MenuItemModel item)
    {
        _menuGrid.ClearSelection();
        foreach (DataGridViewRow row in _menuGrid.Rows)
        {
            if (row.DataBoundItem == item)
            {
                row.Selected = true;
                _menuGrid.CurrentCell = row.Cells[0];
                break;
            }
        }
    }

    private void SelectTable(RestaurantTable table)
    {
        _tableGrid.ClearSelection();
        foreach (DataGridViewRow row in _tableGrid.Rows)
        {
            if (row.DataBoundItem == table)
            {
                row.Selected = true;
                _tableGrid.CurrentCell = row.Cells[0];
                break;
            }
        }
    }
}
