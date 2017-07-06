/**
 * Copyright (c) 2015, OCEAN
 * All rights reserved.
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
 * 1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products derived from this software without specific prior written permission.
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

/**
 * Created by Chen Nan in KETI on 2016-07-28.
 */
using System;
using System.Windows;
using System.Windows.Controls;

namespace MobiusResourceMonitor_sub
{
    /// <summary>
    /// CreateTsWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CreateTsWindow : Window
    {
        public TimeSeriesObject TS;

        public CreateTsWindow()
        {
            InitializeComponent();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (this.txtRN.Text.Trim().Length > 0
                && this.txtMDDT.Text.Length > 0
                && this.txtPEI.Text.Length > 0)
            {
                this.TS = new TimeSeriesObject();
                this.TS.RN = this.txtRN.Text;
                this.TS.PEI = this.txtPEI.Text;

                if (radioTrue.IsChecked.Value)
                {
                    this.TS.MDD = "true";
                }

                if (radioFalse.IsChecked.Value)
                {
                    this.TS.MDD = "false";
                }
                this.TS.MDDT = this.txtMDDT.Text;

                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please input the necessary information for TimeSeries                                                                                                                                                                                                                         ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void txtPEI_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (this.txtPEI.Text.Trim().Length > 0)
                {
                    int value = Convert.ToInt32(this.txtPEI.Text);
                }
            }
            catch
            {
                MessageBox.Show("Must be a number!");
                this.txtPEI.Text = "";
            }
        }

        private void txtMDDT_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (this.txtMDDT.Text.Trim().Length > 0)
                {
                    int value = Convert.ToInt32(this.txtMDDT.Text);
                }
            }
            catch
            {
                MessageBox.Show("Must be a number!");
                this.txtMDDT.Text = "";
            }
        }
    }
}
