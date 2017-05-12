using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.ComponentModel;
using System.Windows.Data;
using System.Globalization;

namespace BatchRename
{
    public class CheckableExtItem : INotifyPropertyChanged
    {
        private string extension;
        public string Extension
        {
            get { return extension; }
            set
            {
                if (extension != value)
                {
                    extension = value;
                    RaisePropertyChanged(nameof(Extension));
                }
            }
        }
        private bool isChecked = false;
        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                if(isChecked != value)
                {
                    isChecked = value;
                    RaisePropertyChanged(nameof(IsChecked));
                }
            }
        }
        private BindingList<string> pathList;
        public BindingList<string> PathList
        {
            get { return pathList; }
            set
            {
                if (pathList != value)
                {
                    pathList = value;
                    RaisePropertyChanged(nameof(PathList));
                }
            }
        }

        public CheckableExtItem()
        {

        }
        public CheckableExtItem(string extension, bool isChecked = false)
        {
            this.Extension = extension;
            this.IsChecked = isChecked;
        }

        protected void RaisePropertyChanged(String propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public struct StateStored<T>
    {
        public T Previous { get; set; }
        public T Current { get; set; }
        public StateStored(T previous, T current)
        {
            Previous = previous;
            Current = current;
        }
    }
    
    /// <summary>
    /// Converts <see cref="bool" /> instances to <see cref="Visibility" /> instances.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BoolToVisibilityConverter" /> class.
        /// </summary>
        public BoolToVisibilityConverter()
        {
            this.InvertVisibility = false;
            this.NotVisibleValue = Visibility.Collapsed;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to invert visibility.
        /// </summary>
        public bool InvertVisibility { get; set; }

        /// <summary>
        /// Gets or sets the not visible value.
        /// </summary>
        /// <value>The not visible value.</value>
        public Visibility NotVisibleValue { get; set; }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns <c>null</c>, the valid <c>null</c> value is used.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Visible;
            }

            bool visible = true;
            if (value is bool)
            {
                visible = (bool)value;
            }

            if (this.InvertVisibility)
            {
                visible = !visible;
            }

            return visible ? Visibility.Visible : this.NotVisibleValue;
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns <c>null</c>, the valid <c>null</c> value is used.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((value is Visibility) && (((Visibility)value) == Visibility.Visible))
                       ? !this.InvertVisibility
                       : this.InvertVisibility;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            System.Windows.Forms.Application.EnableVisualStyles();
        }
        
        private void OpenFolderBtnClick(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = folderDialog.ShowDialog();
            if(result == System.Windows.Forms.DialogResult.OK)
            {
                folder = folderDialog.SelectedPath;
                this.Title = folder;
                EmptyLast();
                LoadFolder();
            }
        }

        private void LoadFolder()
        {
            List<CheckableExtItem> items = null;
            if (Read(folder, out items))
            {
                checkableExtensions = items;
                List00.ItemsSource = checkableExtensions;
                ExtsList.ItemsSource = checkableExtensions;
            }
        }

        private bool Read(string folder, out List<CheckableExtItem> items)
        {
            items = null;
            DirectoryInfo dirInfo = new DirectoryInfo(folder);
            FileInfo[] fileInfos = dirInfo.GetFiles();
            List<string> exts = new List<string>();
            List<CheckableExtItem> extItems = new List<CheckableExtItem>();
            foreach (FileInfo info in fileInfos)
            {
                string extension = info.Extension;
                CheckableExtItem extItem;
                int index = extItems.FindIndex(item => item.Extension.Equals(extension));
                if (index < 0)
                {
                    exts.Add(info.Extension);
                    extItem = new CheckableExtItem(info.Extension);
                    extItem.PathList = new BindingList<string>();
                    extItems.Add(extItem);
                }
                else
                {
                    extItem = extItems[index];
                }
                extItem.PathList.Add(info.FullName);
            }
            items = extItems;
            return true;
        }

        private void ExtensionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasAdded = e.AddedItems != null && e.AddedItems.Count > 0;
            bool hasRemoved = e.RemovedItems != null && e.RemovedItems.Count > 0;
            if (hasAdded || hasRemoved)
            {
                //Update path list of selected extension
                var pathList = new List<string>();
                foreach (var item in List00.SelectedItems)
                {
                    CheckableExtItem checkable = item as CheckableExtItem;
                    if (checkable != null)
                    {
                        pathList.AddRange(checkable.PathList);
                    }
                }
                List01.ItemsSource = pathList;
            }
        }

        private void RenameExtClick(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrEmpty(NewExtTB.Text))
            {
                var pathlist = GetRenameList();
                if(pathlist.Count > 0)
                {
                    bool isDebug = DebugToggle.IsChecked == true;
                    string newExt = NewExtTB.Text;
                    if (!newExt[0].Equals('.'))
                    {
                        newExt = newExt.Insert(0, ".");
                    }
                    List<string> dstPathList = new List<string>();
                    var msgBuilder = new System.Text.StringBuilder();
                    EmptyLast();
                    for (int i = 0; i < pathlist.Count; i++)
                    {
                        string srcPath = pathlist[i];
                        string dstPath = null;
                        int index = srcPath.LastIndexOf('.');
                        if (index > 0)
                        {
                            dstPath = srcPath.Remove(index);
                        }
                        dstPath += newExt;
                        //Rename it
                        try
                        {
                            if (!isDebug)
                            {
                                File.Move(srcPath, dstPath);
                                lastStored.Add(new StateStored<string>(srcPath, dstPath));
                            }
                            msgBuilder.AppendLine(srcPath + " move to " + dstPath);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex.Message);
                            System.Windows.Forms.MessageBox.Show(ex.Message, "Move error");
                        }
                    }
                    if (lastStored.Count > 0)
                        UndoBtn.IsEnabled = true;
                    System.Windows.Forms.MessageBox.Show(msgBuilder.ToString(), "Rename result");
                    //Update list
                    LoadFolder();
                }
            }
        }
        
        private List<string> GetRenameList()
        {
            var list = new List<string>();
            bool byTypes = ByTypesRadioBtn.IsChecked == true;
            if (byTypes)
            {
                foreach (var obj in checkableExtensions)
                {
                    if (obj.IsChecked)
                    {
                        list.AddRange(obj.PathList);
                    }
                }
            }
            else
            {
                var selectedItems = List01.SelectedItems;
                if (selectedItems != null && selectedItems.Count > 0)
                {
                    foreach (var selectedItem in selectedItems)
                    {
                        if(selectedItem is string)
                            list.Add((string)selectedItem);
                    }
                }
            }
            return list;
        }

        private void UndoBtnClick(object sender, RoutedEventArgs e)
        {
            var msgBuilder = new System.Text.StringBuilder();
            foreach (var last in lastStored)
            {
                try
                {
                    File.Move(last.Current, last.Previous);
                    msgBuilder.AppendLine(last.Current + " move to " + last.Previous);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    System.Windows.Forms.MessageBox.Show(ex.Message, "Move error");
                }
            }
            System.Windows.Forms.MessageBox.Show(msgBuilder.ToString(), "Rename result");
            EmptyLast();
            LoadFolder();
        }

        private void EmptyLast()
        {
            lastStored.Clear();
            UndoBtn.IsEnabled = false;
        }

        private string folder;
        private List<CheckableExtItem> checkableExtensions;
        private List<StateStored<string>> lastStored = new List<StateStored<string>>();

    }
}
