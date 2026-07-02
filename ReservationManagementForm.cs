using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using RestaurantManagementApp.Models;

namespace RestaurantManagementApp;

public partial class ReservationManagementForm : Form
{
    private BindingList<Reservation> _reservations = new();
    private Action _saveChanges = () => { };
    private readonly BindingSource _reservationSource = new();
    private int _nextReservationId = 1;

    public ReservationManagementForm()
    {
        InitializeWindow();
        InitializeComponent();
    }

    public ReservationManagementForm(IList<Reservation> reservations, Action saveChanges)
        : this()
    {
        _reservations = new BindingList<Reservation>(reservations);
        _saveChanges = saveChanges;
        _nextReservationId = _reservations.Count > 0
            ? _reservations.Max(item => item.Id) + 1
            : 1;

        InitializeRuntime();
    }

    private void InitializeWindow()
    {
        Text = "إدارة الحجوزات";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(980, 620);
        MinimumSize = new Size(900, 560);
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Font;
    }

    private void InitializeRuntime()
    {
        WireEvents();
        BindData();
        ConfigureReservationGridColumns();
        RestoreSelection();
        UpdateButtons();
        SetStatus("جاهز لإدارة الحجوزات");
    }

    private void WireEvents()
    {
        _reservationGrid.SelectionChanged += (_, _) => PopulateReservationEditorFromSelection();
        _reservationGrid.DataBindingComplete += (_, _) => ConfigureReservationGridColumns();

        _addButton.Click += (_, _) => AddReservation();
        _updateButton.Click += (_, _) => UpdateReservation();
        _deleteButton.Click += (_, _) => DeleteReservation();
        _clearButton.Click += (_, _) => ClearReservationEditor();
        _closeButton.Click += (_, _) => Close();
    }

    private void BindData()
    {
        _reservationSource.DataSource = _reservations;
        _reservationGrid.DataSource = _reservationSource;

        if (_statusCombo.Items.Count == 0)
        {
            _statusCombo.Items.AddRange(new object[] { "مؤكدة", "معلقة", "ملغية" });
        }

        if (_statusCombo.SelectedIndex < 0 && _statusCombo.Items.Count > 0)
        {
            _statusCombo.SelectedIndex = 0;
        }
    }

    private void ConfigureReservationGridColumns()
    {
        ConfigureColumn(_reservationGrid, nameof(Reservation.Id), "الرقم");
        ConfigureColumn(_reservationGrid, nameof(Reservation.CustomerName), "اسم العميل");
        ConfigureColumn(_reservationGrid, nameof(Reservation.Phone), "الهاتف");
        ConfigureColumn(_reservationGrid, nameof(Reservation.GuestsCount), "الضيوف");
        ConfigureColumn(_reservationGrid, nameof(Reservation.ReservedAt), "موعد الحجز", "yyyy-MM-dd HH:mm");
        ConfigureColumn(_reservationGrid, nameof(Reservation.Status), "الحالة");
        ConfigureColumn(_reservationGrid, nameof(Reservation.Notes), "", null, false);
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

    private void RestoreSelection()
    {
        if (_reservationGrid.Rows.Count > 0)
        {
            _reservationGrid.Rows[0].Selected = true;
            _reservationGrid.CurrentCell = _reservationGrid.Rows[0].Cells[0];
            return;
        }

        ClearReservationEditorFields();
    }

    private void PopulateReservationEditorFromSelection()
    {
        if (_reservationGrid.CurrentRow?.DataBoundItem is not Reservation reservation)
        {
            _selectedReservationLabel.Text = "لا يوجد حجز محدد";
            UpdateButtons();
            return;
        }

        _customerNameTextBox.Text = reservation.CustomerName;
        _phoneTextBox.Text = reservation.Phone;
        _guestsUpDown.Value = Math.Min(Math.Max(reservation.GuestsCount, (int)_guestsUpDown.Minimum), (int)_guestsUpDown.Maximum);
        _reservationDateTimePicker.Value = reservation.ReservedAt;
        SelectStatus(reservation.Status);
        _notesTextBox.Text = reservation.Notes;
        _selectedReservationLabel.Text = $"الحجز المختار: #{reservation.Id}";
        UpdateButtons();
    }

    private void AddReservation()
    {
        var customerName = _customerNameTextBox.Text.Trim();
        var phone = _phoneTextBox.Text.Trim();
        var notes = _notesTextBox.Text.Trim();
        var status = GetSelectedStatus();
        var guests = (int)_guestsUpDown.Value;
        var reservedAt = _reservationDateTimePicker.Value;

        if (string.IsNullOrWhiteSpace(customerName))
        {
            ShowWarning("اكتب اسم العميل أولاً.");
            return;
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            ShowWarning("اكتب رقم الهاتف أولاً.");
            return;
        }

        var reservation = new Reservation
        {
            Id = _nextReservationId++,
            CustomerName = customerName,
            Phone = phone,
            GuestsCount = guests,
            ReservedAt = reservedAt,
            Status = status,
            Notes = notes
        };

        _reservations.Add(reservation);
        _reservationSource.ResetBindings(false);
        SelectReservation(reservation);
        CommitChanges("تمت إضافة الحجز");
    }

    private void UpdateReservation()
    {
        if (_reservationGrid.CurrentRow?.DataBoundItem is not Reservation reservation)
        {
            ShowWarning("اختر حجزاً من الجدول أولاً.");
            return;
        }

        var customerName = _customerNameTextBox.Text.Trim();
        var phone = _phoneTextBox.Text.Trim();
        var notes = _notesTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(customerName))
        {
            ShowWarning("اكتب اسم العميل أولاً.");
            return;
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            ShowWarning("اكتب رقم الهاتف أولاً.");
            return;
        }

        reservation.CustomerName = customerName;
        reservation.Phone = phone;
        reservation.GuestsCount = (int)_guestsUpDown.Value;
        reservation.ReservedAt = _reservationDateTimePicker.Value;
        reservation.Status = GetSelectedStatus();
        reservation.Notes = notes;

        _reservationSource.ResetBindings(false);
        PopulateReservationEditorFromSelection();
        CommitChanges("تم تحديث الحجز");
    }

    private void DeleteReservation()
    {
        if (_reservationGrid.CurrentRow?.DataBoundItem is not Reservation reservation)
        {
            ShowWarning("اختر حجزاً من الجدول أولاً.");
            return;
        }

        if (MessageBox.Show(this, $"هل تريد حذف الحجز الخاص بـ '{reservation.CustomerName}'؟", "تأكيد الحذف",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        _reservations.Remove(reservation);
        _reservationSource.ResetBindings(false);
        ClearReservationEditor();
        CommitChanges("تم حذف الحجز");
    }

    private void ClearReservationEditor()
    {
        ClearReservationEditorFields();
        _reservationGrid.ClearSelection();
        _reservationGrid.CurrentCell = null;
        UpdateButtons();
    }

    private void ClearReservationEditorFields()
    {
        _customerNameTextBox.Clear();
        _phoneTextBox.Clear();
        _guestsUpDown.Value = 2;
        _reservationDateTimePicker.Value = DateTime.Now;

        if (_statusCombo.Items.Count > 0)
        {
            _statusCombo.SelectedIndex = 0;
        }

        _notesTextBox.Clear();
        _selectedReservationLabel.Text = "لا يوجد حجز محدد";
    }

    private void UpdateButtons()
    {
        var hasSelection = _reservationGrid.CurrentRow?.DataBoundItem is Reservation;
        _updateButton.Enabled = hasSelection;
        _deleteButton.Enabled = hasSelection;
    }

    private void CommitChanges(string statusMessage)
    {
        try
        {
            _saveChanges();
            SetStatus(statusMessage);
        }
        catch (Exception ex)
        {
            ShowError($"تعذر حفظ البيانات.\n{ex.Message}");
        }
    }

    private string GetSelectedStatus()
    {
        if (_statusCombo.SelectedItem is string status && !string.IsNullOrWhiteSpace(status))
        {
            return status;
        }

        return _statusCombo.Items.Count > 0
            ? _statusCombo.Items[0] as string ?? "مؤكدة"
            : "مؤكدة";
    }

    private void SelectStatus(string status)
    {
        foreach (var item in _statusCombo.Items)
        {
            if (string.Equals(item?.ToString(), status, StringComparison.OrdinalIgnoreCase))
            {
                _statusCombo.SelectedItem = item;
                return;
            }
        }

        if (_statusCombo.Items.Count > 0)
        {
            _statusCombo.SelectedIndex = 0;
        }
    }

    private void SelectReservation(Reservation reservation)
    {
        _reservationGrid.ClearSelection();

        foreach (DataGridViewRow row in _reservationGrid.Rows)
        {
            if (row.DataBoundItem == reservation)
            {
                row.Selected = true;
                _reservationGrid.CurrentCell = row.Cells[0];
                break;
            }
        }

        PopulateReservationEditorFromSelection();
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
    }

    private static void ShowWarning(string message)
    {
        MessageBox.Show(message, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ShowError(string message)
    {
        MessageBox.Show(this, message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
