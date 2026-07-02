using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using RestaurantManagementApp.Models;
using MenuItemModel = RestaurantManagementApp.Models.MenuItem;

namespace RestaurantManagementApp;

public sealed class CategoryManagementForm : Form
{
    private readonly BindingList<Category> _categories;
    private readonly BindingList<MenuItemModel> _menuItems;
    private readonly Action _saveChanges;
    private readonly BindingSource _categorySource = new();

    private ListBox _categoryListBox = null!;
    private TextBox _categoryNameTextBox = null!;
    private Label _selectedCategoryLabel = null!;

    private Button _addButton = null!;
    private Button _updateButton = null!;
    private Button _deleteButton = null!;
    private Button _clearButton = null!;
    private Button _closeButton = null!;

    private ToolStripStatusLabel _statusLabel = null!;

    public CategoryManagementForm(
        BindingList<Category> categories,
        BindingList<MenuItemModel> menuItems,
        Action saveChanges)
    {
        _categories = categories;
        _menuItems = menuItems;
        _saveChanges = saveChanges;

        InitializeWindow();
        BuildUi();
        WireEvents();
        BindData();
        RestoreSelection();
        UpdateButtons();
        SetStatus("جاهز لإدارة الأنواع");
    }

    private void InitializeWindow()
    {
        Text = "إدارة الأنواع";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(820, 520);
        MinimumSize = new Size(760, 470);
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

        root.Controls.Add(CreateHeaderPanel(), 0, 0);
        root.Controls.Add(CreateBodyPanel(), 0, 1);
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

        var title = new Label
        {
            Text = "إدارة الأنواع",
            Dock = DockStyle.Top,
            Height = 30,
            ForeColor = Color.White,
            Font = new Font(Font.FontFamily, 18F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var subtitle = new Label
        {
            Text = "أضف أو عدّل أو احذف أنواع الأصناف، وسيتم تحديث الأصناف المرتبطة تلقائيًا.",
            Dock = DockStyle.Fill,
            ForeColor = Color.Gainsboro,
            Font = new Font(Font.FontFamily, 10F),
            TextAlign = ContentAlignment.MiddleLeft
        };

        panel.Controls.Add(subtitle);
        panel.Controls.Add(title);
        return panel;
    }

    private Control CreateBodyPanel()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterDistance = 300,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.WhiteSmoke
        };
        split.Panel1.Padding = new Padding(12, 12, 6, 12);
        split.Panel2.Padding = new Padding(6, 12, 12, 12);

        split.Panel1.Controls.Add(CreateCategoryListGroup());
        split.Panel2.Controls.Add(CreateEditorGroup());
        return split;
    }

    private GroupBox CreateCategoryListGroup()
    {
        var group = new GroupBox
        {
            Text = "الأنواع الحالية",
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        _categoryListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            BorderStyle = BorderStyle.FixedSingle
        };

        group.Controls.Add(_categoryListBox);
        return group;
    }

    private GroupBox CreateEditorGroup()
    {
        var group = new GroupBox
        {
            Text = "بيانات النوع",
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        var layout = CreateInputTable(4);

        _categoryNameTextBox = new TextBox();
        _selectedCategoryLabel = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(75, 85, 99),
            TextAlign = ContentAlignment.MiddleLeft
        };

        AddLabeledRow(layout, 0, "اسم النوع", _categoryNameTextBox);
        AddLabeledRow(layout, 1, "النوع المختار", _selectedCategoryLabel);

        _addButton = CreateButton("إضافة", Color.FromArgb(46, 204, 113));
        _updateButton = CreateButton("تحديث", Color.FromArgb(52, 152, 219));
        _deleteButton = CreateButton("حذف", Color.FromArgb(231, 76, 60));
        _clearButton = CreateButton("تفريغ", Color.FromArgb(149, 165, 166));
        _closeButton = CreateButton("إغلاق", Color.FromArgb(100, 116, 139));

        AddFullRow(layout, 2, CreateButtonRow(_addButton, _updateButton, _deleteButton, _clearButton), 88);
        AddFullRow(layout, 3, _closeButton, 44);

        group.Controls.Add(layout);
        return group;
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

        strip.Items.Add(_statusLabel);
        return strip;
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

    private void WireEvents()
    {
        _categoryListBox.SelectedIndexChanged += (_, _) => PopulateEditorFromSelection();

        _addButton.Click += (_, _) => AddCategory();
        _updateButton.Click += (_, _) => UpdateCategory();
        _deleteButton.Click += (_, _) => DeleteCategory();
        _clearButton.Click += (_, _) => ClearEditor();
        _closeButton.Click += (_, _) => Close();

        _categoryNameTextBox.TextChanged += (_, _) => UpdateButtons();
    }

    private void BindData()
    {
        _categorySource.DataSource = _categories;
        _categoryListBox.DataSource = _categorySource;
        _categoryListBox.DisplayMember = nameof(Category.Name);
    }

    private void RestoreSelection()
    {
        if (_categories.Count > 0)
        {
            _categoryListBox.SelectedIndex = 0;
        }
    }

    private void PopulateEditorFromSelection()
    {
        if (_categoryListBox.SelectedItem is not Category category)
        {
            _categoryNameTextBox.Clear();
            _selectedCategoryLabel.Text = "لا يوجد";
            UpdateButtons();
            return;
        }

        _categoryNameTextBox.Text = category.Name;
        _selectedCategoryLabel.Text = $"#{category.Id}";
        UpdateButtons();
    }

    private void AddCategory()
    {
        var name = _categoryNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowWarning("اكتب اسم النوع أولًا.");
            return;
        }

        if (CategoryNameExists(name))
        {
            ShowWarning("هذا النوع موجود بالفعل.");
            return;
        }

        var category = new Category
        {
            Id = GetNextCategoryId(),
            Name = name
        };

        _categories.Add(category);
        _categorySource.ResetBindings(false);
        _categoryListBox.SelectedItem = category;
        _saveChanges();
        _categorySource.ResetBindings(false);
        SetStatus("تمت إضافة النوع");
    }

    private void UpdateCategory()
    {
        if (_categoryListBox.SelectedItem is not Category category)
        {
            ShowWarning("اختر نوعًا من القائمة أولًا.");
            return;
        }

        var name = _categoryNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowWarning("اكتب اسم النوع أولًا.");
            return;
        }

        if (CategoryNameExists(name, category))
        {
            ShowWarning("يوجد نوع آخر بنفس الاسم.");
            return;
        }

        var oldName = category.Name;
        category.Name = name;

        foreach (var menuItem in _menuItems)
        {
            if (string.Equals(menuItem.Category, oldName, StringComparison.OrdinalIgnoreCase))
            {
                menuItem.Category = name;
            }
        }

        _categorySource.ResetBindings(false);
        _saveChanges();
        _categorySource.ResetBindings(false);
        SetStatus("تم تحديث النوع");
    }

    private void DeleteCategory()
    {
        if (_categoryListBox.SelectedItem is not Category category)
        {
            ShowWarning("اختر نوعًا من القائمة أولًا.");
            return;
        }

        if (_menuItems.Any(menuItem => string.Equals(menuItem.Category, category.Name, StringComparison.OrdinalIgnoreCase)))
        {
            ShowWarning("لا يمكن حذف نوع مستخدم داخل الأصناف. غيّر الأصناف المرتبطة أولًا.");
            return;
        }

        if (MessageBox.Show(this, $"هل تريد حذف النوع '{category.Name}'؟", "تأكيد الحذف",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        _categories.Remove(category);
        _categorySource.ResetBindings(false);
        RestoreSelection();
        _saveChanges();
        _categorySource.ResetBindings(false);
        SetStatus("تم حذف النوع");
    }

    private void ClearEditor()
    {
        _categoryListBox.ClearSelected();
        _categoryNameTextBox.Clear();
        _selectedCategoryLabel.Text = "لا يوجد";
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var hasSelection = _categoryListBox.SelectedItem is Category;
        var hasText = !string.IsNullOrWhiteSpace(_categoryNameTextBox.Text);

        _addButton.Enabled = hasText;
        _updateButton.Enabled = hasSelection && hasText;
        _deleteButton.Enabled = hasSelection;
    }

    private bool CategoryNameExists(string name, Category? ignore = null)
    {
        return _categories.Any(category =>
            !ReferenceEquals(category, ignore) &&
            string.Equals(category.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private int GetNextCategoryId()
    {
        return _categories.Count == 0 ? 1 : _categories.Max(category => category.Id) + 1;
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
    }

    private static void ShowWarning(string message)
    {
        MessageBox.Show(message, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
