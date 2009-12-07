﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Plugins;
using System.IO;

namespace ImageQueue
{
    public partial class Form1 : Form
    {
        private List<IPluginInterface> plugins;
        private Image previewImage;

        public Form1()
        {
            InitializeComponent();
            string pluginsPath = Path.Combine(Application.StartupPath, "Plugins");
            plugins = PluginManager.LoadPlugins<IPluginInterface>(pluginsPath);
            FillPluginsList();
            previewImage = ImageQueue.Properties.Resources.main;
            pbPreview.Image = previewImage;
        }

        private void FillPluginsList()
        {
            TreeNode parentNode, childNode;
            foreach (IPluginInterface plugin in plugins)
            {
                parentNode = tvPlugins.Nodes.Add(plugin.Name);
                parentNode.Tag = plugin;

                foreach (IPluginItem item in plugin.PluginItems)
                {
                    childNode = parentNode.Nodes.Add(item.Name);
                    childNode.Tag = item;
                }
            }

            tvPlugins.ExpandAll();
        }

        private void lvEffects_SelectedIndexChanged(object sender, EventArgs e)
        {
            pgSettings.SelectedObject = null;

            if (lvEffects.SelectedItems.Count > 0)
            {
                ListViewItem lvi = lvEffects.SelectedItems[0];
                if (lvi.Tag is IPluginItem)
                {
                    pgSettings.SelectedObject = lvi.Tag;
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            TreeNode node = tvPlugins.SelectedNode;
            if (node.Tag is IPluginItem)
            {
                IPluginItem pluginItem = (IPluginItem)Activator.CreateInstance(node.Tag.GetType());
                ListViewItem lvi = new ListViewItem(pluginItem.Name);
                lvi.Tag = pluginItem;
                pluginItem.PreviewTextChanged += p => lvi.Text = string.Format("{0}: {1}", pluginItem.Name, p);
                lvEffects.Items.Add(lvi);
            }

            UpdatePreview();
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in lvEffects.SelectedItems)
            {
                lvi.Remove();
            }

            UpdatePreview();
        }

        private void pgSettings_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            Image img = (Image)previewImage.Clone();

            foreach (ListViewItem lvi in lvEffects.Items)
            {
                if (lvi.Tag is IPluginItem)
                {
                    ((IPluginItem)lvi.Tag).ApplyEffect(img);
                }
            }

            pbPreview.Image = img;
        }
    }
}