/* 
MIT License

Copyright (c) 2016-2017 Florian C�sar, Michael Plainer

For full license see LICENSE in the root directory of this project. 
*/

using System.Windows.Controls;

namespace Sigma.Core.Monitors.WPF.Panels.DataGrids
{
	public abstract class SigmaDataGridPanel : GenericPanel<DataGrid>
	{
		protected SigmaDataGridPanel(string title, object content = null) : base(title, content)
		{
			Content = new DataGrid
			{
				IsReadOnly = true,
				ClipboardCopyMode = DataGridClipboardCopyMode.ExcludeHeader
			};
		}
	}
}