using System;
using System.Windows;
using System.Windows.Controls;
using System.Data;
using System.Collections.Generic;
using DBConnect;

namespace Employees
{
    public partial class MainWindow : Window
    {
        private DB conn;
        private DataTable dt;
        private bool isTableSelected = false;

        string selTableName;
        public MainWindow()
        {
            InitializeComponent();
        }
        //обрабатывает нажатие на кнопку conn
        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                conn = new DB(DBNameTxtBox.Text, UsernameTxtBox.Text, PwdBox.Password);
                conn.DbConnect();
                ConnectBtn.IsEnabled = false;
                DisconnectBtn.IsEnabled = true;

                DataTable tmp = conn.execute("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'; ");
                foreach (DataRow row in tmp.Rows)
                {
                    for (int i = 0; i < tmp.Columns.Count; i++)
                    {
                        TablesList.Items.Add(row[i].ToString());
                    }
                }

                tmp = conn.execute("select routine_name, routine_type from information_schema.routines where routine_schema not in ('pg_catalog', 'information_schema') and routine_name like 'query%'");
                foreach (DataRow row in tmp.Rows)
                {
                    ProceduresList.Items.Add((row[1].ToString() == "FUNCTION" ? "F_" : "P_") + row[0].ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                MessageBox.Show(ex.Message, "Ошибка при подключении!");
            }
        }
        //обрабатывает нажатие на кнопку dconn
        private void DisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            conn.Disconnect();
            TablesList.Items.Clear();
            ProceduresList.Items.Clear();
            DbGrid.ItemsSource = null;
            conn = null;
            selTableName = null;
            ConnectBtn.IsEnabled = true;
            DisconnectBtn.IsEnabled = false;

            DelRecordBtn.IsEnabled = false;
            AddRecordBtn.IsEnabled = false;
        }
        // обрабатывает нажатие на список таблиц
        private void TablesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TablesList.SelectedItem != null)
            {
                string tableName = TablesList.SelectedItem.ToString();
                try
                {
                    dt = conn.execute("SELECT * FROM " + tableName);
                    DbGrid.ItemsSource = dt.DefaultView;
                    selTableName = tableName;

                    DelRecordBtn.IsEnabled = true;
                    AddRecordBtn.IsEnabled = true;

                    isTableSelected = true;

                    TablesList.SelectedItem = null;
                    ProceduresList.SelectedItem = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
        //нажатие на список процедур
        private void ProceduresList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProceduresList.SelectedItem != null)
            {
                string tableName = ProceduresList.SelectedItem.ToString();
                try
                {
                    string routineName = ProceduresList.SelectedItem.ToString().Substring(2);
                    string query = "select p.parameter_name " +
                                                    "from information_schema.routines r left join information_schema.parameters p on r.specific_schema = p.specific_schema and r.specific_name = p.specific_name " +
                                                    "where r.routine_schema not in ('pg_catalog', 'information_schema') AND p.parameter_mode = 'IN' AND r.routine_name = \'" + routineName + "\' order by p.ordinal_position;";
                    dt = conn.execute(query);
                    isTableSelected = false;

                    if (dt.Rows.Count == 0)
                    {
                        string prefix = ProceduresList.SelectedItem.ToString().Substring(0, 2);
                        query = (prefix == "F_" ? "SELECT * FROM " : "CALL ") + routineName + "();";
                        Console.WriteLine(query);
                        dt = conn.execute(query);
                        DbGrid.ItemsSource = dt.DefaultView;
                        selTableName = tableName;
                    }
                    else
                    {
                        List<string> headers = new List<string>();
                        for(int i = 0; i < dt.Rows.Count; ++i)
                        {
                            headers.Add(dt.Rows[i].Field<string>(0));
                        }
                        ParametersWindow insert = new ParametersWindow(conn, dt, DbGrid, headers, ProceduresList.SelectedItem.ToString(), "ROUTINE");
                       
                        insert.ShowDialog();
                    }

                    TablesList.SelectedItem = null;
                    ProceduresList.SelectedItem = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
        // генерация столбцов
        private void DbGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType == typeof(System.DateTime))
                (e.Column as DataGridTextColumn).Binding.StringFormat = "yyyy-MM-dd";
        }
        //обработка нажатия на строчки в таблице
        private void DbGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (isTableSelected)
            {
                if (selTableName != null)
                {
                    string value = "";
                    if (e.EditingElement is TextBox castedTB)
                    {
                        value = '\'' + castedTB.Text.ToString().Replace("\'", "") + '\'';
                    } else if (e.EditingElement is CheckBox castedCB)
                    {
                        value = '\'' + castedCB.IsChecked.ToString().Replace("\'", "") + '\'';
                    }

                    string query = "select * from " + selTableName + "_update(";

                    for (int i = 0; i < ((DataGrid)sender).Columns.Count; ++i)
                    {
                        if (((DataGrid)sender).Columns[i].Header == e.Column.Header)
                            query += value;
                        else
                            query += '\'' + ((DataRowView)e.Row.Item)[i].ToString().Replace("\'", "") + "\'";
                        query += ", ";
                    }
                    query = query.Substring(0, query.Length - 2) + ");";

                    try
                    {
                        conn.execute(query);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при обновлении!\n" + ex, "SQL Error");
                    }
                }
                else
                {
                    MessageBox.Show("Не выбрана таблица", "Внимание!");
                }
            }
        }
        //добавление записи
        private void AddRecordBtn_Click(object sender, RoutedEventArgs e)
        {
            if (selTableName != null)
            {
                try
                {
                    List<string> headers = new List<string>();
                    foreach (var column in DbGrid.Columns)
                    {
                        headers.Add(column.Header.ToString());
                    }
                    ParametersWindow insert = new ParametersWindow(conn, dt, DbGrid, headers, selTableName, "INSERT");
                    insert.ShowDialog();
                    string query = "SELECT * FROM \"" + selTableName + "\"";
                    dt = conn.execute(query);
                    DbGrid.ItemsSource = dt.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при вставке!\n" + ex.Message, "Ошибка");
                }
            }
        }
        //удаление записи
        private void DelRecordBtn_Click(object sender, RoutedEventArgs e)
        {
            if (selTableName != null && DbGrid.Items.Count > 0 && DbGrid.SelectedItem != null)
            {
                try
                {
                    string query = "SELECT * FROM " + selTableName + "_delete(" + ((DataRowView)DbGrid.SelectedItem)[0].ToString()  + "); SELECT * FROM " + selTableName + ";";
                    dt = conn.execute(query);
                    DbGrid.ItemsSource = dt.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении!\n" + ex.Message, "Ошибка");
                }
            }
        }
    }
}
