using System;
using System.Collections.ObjectModel;
using DBConnect;
using System.Data;

using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
namespace Employees
{
    /// <summary>
    /// Логика взаимодействия для ParametersWindow.xaml
    /// </summary>
    public partial class ParametersWindow : Window
    {
        private string entityName;
        private DB conn;
        private DataTable dt;
        private DataGrid DbGrid;
        List<TextBox> TextBoxList = new List<TextBox>();
        //создание окна с парметрами 
        public ParametersWindow(DB _conn, DataTable _dt, DataGrid _DbGrid, List<string> headers, string _entityName, string operation)
        {
            InitializeComponent();
            
            entityName = _entityName;
            conn = _conn;
            dt = _dt;
            DbGrid = _DbGrid;

            int margin = 0;
            int offset = 1;
            if (operation == "ROUTINE")
                offset = 0;
            if (operation == "INSERT")
                offset = 1;

            for (int i = offset; i < headers.Count; ++i)
            {
                Label lbl = new Label();
                lbl = (Label)GenerateControl(lbl, headers[i], new Thickness(10, margin, 0, 0));
                
                margin += 30;
                InsertGrid.Children.Add(lbl);

                TextBox txt = new TextBox();
                txt = (TextBox)GenerateControl(txt, headers[i], new Thickness(10, margin, 0, 0));
                
                InsertGrid.Children.Add(txt);

                margin += 30;

                TextBoxList.Add(txt);
            }

            Button btn = new Button();
            btn = (Button)GenerateControl(btn, "Выполнить запрос", new Thickness(10, margin + 30, 0, 30));
            if (operation == "ROUTINE")
                btn.Click += RoutineBtn_Click;
            if (operation == "INSERT")
                btn.Click += InsertBtn_Click;

            InsertGrid.Children.Add(btn);
        }
        //для кнопки
        private object GenerateControl(ContentControl control, string header, Thickness margin)
        {
            control.Content = header;
            control.Width = 120;
            control.Height = 35;
            control.Margin = margin;
            control.HorizontalAlignment = HorizontalAlignment.Left;
            control.VerticalAlignment = VerticalAlignment.Top;

            return control;
        }
        // для тексбокс
        private object GenerateControl(Control control, string header, Thickness margin)
        {
            control.Name = header.Replace(" ", string.Empty) + "_TextBox";
            control.Width = 120;
            control.Height = 35;
            control.Margin = margin;
            control.HorizontalAlignment = HorizontalAlignment.Left;
            control.VerticalAlignment = VerticalAlignment.Top;

            return control;
        }
        //если создается окно для добавления строки то вешается этот обработчик 
        private void InsertBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string query = "SELECT * FROM " + entityName + "_insert(";

                foreach (var textBox in TextBoxList)
                {
                    query += '\'' + textBox.Text.Replace("\'", "") + "\' ,";
                }
                query = query.Substring(0, query.Length - 2);
                query += ");";
                dt = conn.execute(query);

                MessageBox.Show("Выполнен запрос");

                GetWindow(this).Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при выполнении запроса!\n" + ex.Message, "Ошибка");
            }
        }
        //если создается окно для запроса  то вешается этот обработчик
        private void RoutineBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string prefix = entityName.Substring(0, 2);
                string query = (prefix == "F_" ? "SELECT * FROM " : "CALL ") + entityName.Substring(2) + "(";

                foreach (var textBox in TextBoxList)
                {
                    query += '\'' + textBox.Text.Replace("\'", "") + "\' ,";
                }
                query = query.Substring(0, query.Length - 2);
                query += ");";
                dt = conn.execute(query);
                
                MessageBox.Show("Выполнен запрос");
                
                GetWindow(this).Close();

                DbGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при выполнении запроса!\n" + ex.Message, "Ошибка");
            }
        }
    }
}
