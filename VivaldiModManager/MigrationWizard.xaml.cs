﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;

namespace VivaldiModManager
{
    /// <summary>
    /// Логика взаимодействия для MigrationWizard.xaml
    /// </summary>
    public partial class MigrationWizard : MetroWindow
    {
        public bool StartMigration = false;
        public MigrationWizard()
        {
            InitializeComponent();
        }

        private void migrateButton_Click(object sender, RoutedEventArgs e)
        {
            this.StartMigration = true;
            this.Close();
        }
    }
}