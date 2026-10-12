using System.Diagnostics;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace DedupeLight;

public sealed class MainForm : Form
{
    static readonly Color Ink=Color.FromArgb(24,27,34), Muted=Color.FromArgb(100,108,122), Line=Color.FromArgb(229,232,238), Accent=Color.FromArgb(0,113,227), Surface=Color.White, Canvas=Color.FromArgb(245,245,247);
    readonly TextBox pathBox=new(){PlaceholderText="Choose a folder or drive",Dock=DockStyle.Fill,BorderStyle=BorderStyle.FixedSingle};
    readonly TextBox searchBox=new(){PlaceholderText="Filter results",Width=230,BorderStyle=BorderStyle.FixedSingle};
    readonly Button browse=MakeButton("Browse",true),scan=MakeButton("Scan now"),cancel=MakeButton("Cancel",true),selectExtras=MakeButton("Select extras",true),clearSelection=MakeButton("Clear",true),recycle=MakeButton("Recycle",false,true),move=MakeButton("Move",true),copy=MakeButton("Copy",true),open=MakeButton("Open",true),reveal=MakeButton("Show in Explorer",true),export=MakeButton("Export CSV",true);
    readonly Button preview=MakeButton("Review",true),excludeFile=MakeButton("Exclude file",true),excludeFolder=MakeButton("Exclude folder",true),manageExclusions=MakeButton("Excluded sources",true);
    List<ExclusionRule> exclusions=[];
    readonly Label fileDetails=new(){Dock=DockStyle.Fill,ForeColor=Muted,AutoEllipsis=true,TextAlign=ContentAlignment.MiddleLeft};
    readonly ToolTip hints=new();
    readonly ComboBox keepRule=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=142,Height=34};
    readonly NumericUpDown minimumSize=new(){Minimum=0,Maximum=102400,Value=1,Width=82,ThousandsSeparator=true};
    readonly CheckBox hiddenSystem=new(){Text="Include hidden/system",AutoSize=true,ForeColor=Muted,Padding=new Padding(4,6,0,0)};
    readonly DataGridView grid=new SmoothGrid(){Dock=DockStyle.Fill,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AutoGenerateColumns=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=true,RowHeadersVisible=false,BorderStyle=BorderStyle.None,BackgroundColor=Surface,GridColor=Line,CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal};
    readonly Label status=new(){Text="Choose a location to begin",AutoSize=true,ForeColor=Muted},summary=new(){Text="No scan yet",AutoSize=true,Font=new Font("Segoe UI Semibold",10.5f),ForeColor=Ink},selectedSummary=new(){Text="",AutoSize=true,ForeColor=Muted,Padding=new Padding(10,7,0,0)};
    readonly ProgressBar progress=new(){Dock=DockStyle.Bottom,Height=3,Style=ProgressBarStyle.Marquee,Visible=false,MarqueeAnimationSpeed=24};
    readonly string appDataPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DedupeLight"); readonly BindingSource source=new();
    string CachePath=>Path.Combine(appDataPath,"hash-cache.json"); string SettingsPath=>Path.Combine(appDataPath,"settings.json");
    List<DuplicateItem> allItems=[]; CancellationTokenSource? cts; int sortColumn=0; bool sortAscending=true; bool busy, rebinding;

    public MainForm(){Text="DedupeLight";Icon=System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);Width=1180;Height=800;MinimumSize=new Size(940,640);AutoScaleMode=AutoScaleMode.Dpi;DoubleBuffered=true;StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10f);BackColor=Canvas;ForeColor=Ink;AllowDrop=true;keepRule.Items.AddRange(["Keep oldest","Keep newest","Keep shortest path"]);keepRule.SelectedIndex=0;BuildLayout();ConfigureGrid();WireEvents();LoadPreferences();SetBusy(false);UpdateSummary();}
    readonly Label emptyState=new(){Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Muted,Font=new Font("Segoe UI",12),Text="A little more room for what matters.\n\nChoose a folder or drop one here to find exact duplicates."};
    readonly System.Windows.Forms.Timer filterDelay=new(){Interval=180};
    bool hasScanned;
    static Button MakeButton(string text,bool secondary=false,bool danger=false)=>new SoftButton{Text=text,Primary=!secondary&&!danger,Destructive=danger};
    void BuildLayout()
    {
        SuspendLayout();
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,BackColor=Canvas,Padding=new Padding(26,14,26,20),ColumnCount=1,RowCount=6};
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,82));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,124));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,76));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,148));
        var header=new Panel{Dock=DockStyle.Fill};
        header.Controls.AddRange([new Label{Text="DedupeLight",AutoSize=true,Font=new Font("Segoe UI Semibold",23),ForeColor=Ink,Location=new Point(0,0)},new Label{Text="Less clutter. More space.",AutoSize=true,ForeColor=Muted,Location=new Point(3,47)}]);
        var about=MakeButton("About",true);about.Anchor=AnchorStyles.Top|AnchorStyles.Right;about.Location=new Point(Width-160,8);
        about.Click+=(_,_)=>MessageBox.Show(this,"DedupeLight 1.0.0\nLess clutter. More space.\n\nCreated by Ismail Hossain · MIT license\nExact duplicates only · local scanning\n\ngithub.com/smile-plzz/dedupe-light","About DedupeLight",MessageBoxButtons.OK,MessageBoxIcon.Information);
        header.Controls.Add(about);header.Resize+=(_,_)=>about.Location=new Point(Math.Max(0,header.ClientSize.Width-about.Width-4),8);
        layout.Controls.Add(header,0,0);
        var location=new SurfaceCard{Dock=DockStyle.Fill,Margin=new Padding(0,0,0,4)};
        var fields=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=2,BackColor=Surface};
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));for(var i=0;i<3;i++)fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute,42));fields.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        pathBox.Margin=new Padding(0,6,12,0);pathBox.AccessibleName="Folder or drive to scan";
        fields.Controls.Add(pathBox,0,0);fields.Controls.Add(browse,1,0);fields.Controls.Add(scan,2,0);fields.Controls.Add(cancel,3,0);
        var options=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Margin=new Padding(0,8,0,0)};
        options.Controls.Add(new Label{Text="Minimum size",AutoSize=true,ForeColor=Muted,Padding=new Padding(0,4,8,0)});options.Controls.Add(minimumSize);
        options.Controls.Add(new Label{Text="MB",AutoSize=true,ForeColor=Muted,Padding=new Padding(0,4,18,0)});options.Controls.Add(hiddenSystem);options.Controls.Add(manageExclusions);
        fields.SetColumnSpan(options,4);fields.Controls.Add(options,0,1);location.Controls.Add(fields);layout.Controls.Add(location,0,1);
        var resultHead=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(2,12,0,8),ColumnCount=2};
        resultHead.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));resultHead.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,250));
        var labels=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty};
        labels.RowStyles.Add(new RowStyle(SizeType.Absolute,24));labels.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        summary.Dock=DockStyle.Fill;summary.AutoSize=false;summary.AutoEllipsis=true;
        status.Dock=DockStyle.Fill;status.AutoSize=false;status.AutoEllipsis=true;
        labels.Controls.Add(summary,0,0);labels.Controls.Add(status,0,1);resultHead.Controls.Add(labels,0,0);
        searchBox.Dock=DockStyle.Top;searchBox.Margin=new Padding(12,3,0,0);searchBox.PlaceholderText="Search name or location";searchBox.AccessibleName="Search duplicate results";
        resultHead.Controls.Add(searchBox,1,0);layout.Controls.Add(resultHead,0,2);
        var host=new SurfaceCard{Dock=DockStyle.Fill,Padding=new Padding(12),Margin=Padding.Empty};
        host.Controls.Add(grid);host.Controls.Add(emptyState);layout.Controls.Add(host,0,3);layout.Controls.Add(fileDetails,0,4);
        var actions=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(0,8,0,0),Margin=Padding.Empty};
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute,43));actions.RowStyles.Add(new RowStyle(SizeType.Absolute,43));actions.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var selection=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Margin=Padding.Empty};
        selection.Controls.AddRange([keepRule,selectExtras,clearSelection,selectedSummary]);
        var tools=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Margin=Padding.Empty};
        tools.Controls.AddRange([recycle,move,copy,preview,open,reveal,export]);
        var protection=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Margin=Padding.Empty};protection.Controls.AddRange([excludeFile,excludeFolder,new Label{Text="Intentional copies? Exclude a file or its folder.",AutoSize=true,ForeColor=Muted,Padding=new Padding(0,8,0,0)}]);
        actions.Controls.Add(selection,0,0);actions.Controls.Add(tools,0,1);actions.Controls.Add(protection,0,2);layout.Controls.Add(actions,0,5);
        hints.SetToolTip(excludeFile,"Skip selected files in this scan and future scans. Files stay on disk.");
        hints.SetToolTip(excludeFolder,"Skip the entire containing folder, including subfolders.");
        hints.SetToolTip(preview,"Review a highlighted file. Double-click opens it in the default app.");
        Controls.Add(layout);Controls.Add(progress);ResumeLayout(true);
    }
    void ConfigureGrid(){grid.EnableHeadersVisualStyles=false;grid.ColumnHeadersHeight=44;grid.RowTemplate.Height=42;grid.AllowUserToResizeRows=false;grid.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.None;grid.ColumnHeadersDefaultCellStyle=new(){BackColor=Color.FromArgb(248,249,251),ForeColor=Muted,Font=new Font("Segoe UI Semibold",9),SelectionBackColor=Color.FromArgb(248,249,251),Padding=new Padding(4)};grid.DefaultCellStyle=new(){BackColor=Surface,ForeColor=Ink,SelectionBackColor=Color.FromArgb(232,242,255),SelectionForeColor=Ink,Padding=new Padding(4),NullValue=""};grid.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(251,252,253);grid.Columns.Add(new DataGridViewCheckBoxColumn{DataPropertyName=nameof(DuplicateItem.Selected),HeaderText="",Width=42});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=nameof(DuplicateItem.Group),HeaderText="Set",Width=58,ReadOnly=true});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=nameof(DuplicateItem.Name),HeaderText="File",Width=240,ReadOnly=true});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=nameof(DuplicateItem.Folder),HeaderText="Location",AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill,ReadOnly=true});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=nameof(DuplicateItem.SizeText),HeaderText="Size",Width=92,ReadOnly=true});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=nameof(DuplicateItem.ModifiedText),HeaderText="Modified",Width=140,ReadOnly=true});grid.DataSource=source;}
    void WireEvents(){browse.Click+=(_,_)=>ChooseFolder();scan.Click+=async(_,_)=>await ScanAsync();cancel.Click+=(_,_)=>cts?.Cancel();selectExtras.Click+=(_,_)=>SelectExtras();clearSelection.Click+=(_,_)=>{allItems.ForEach(x=>x.Selected=false);grid.Refresh();UpdateActions();};recycle.Click+=async(_,_)=>await RecycleAsync();move.Click+=async(_,_)=>await TransferAsync(true);copy.Click+=async(_,_)=>await TransferAsync(false);open.Click+=(_,_)=>OpenFile();reveal.Click+=(_,_)=>Reveal();export.Click+=async(_,_)=>await ExportCsvAsync();searchBox.TextChanged+=(_,_)=>{filterDelay.Stop();filterDelay.Start();};filterDelay.Tick+=(_,_)=>{filterDelay.Stop();if(!busy)ApplyFilter();};grid.SelectionChanged+=(_,_)=>UpdateActions();grid.CellValueChanged+=(_,_)=>UpdateActions();grid.CurrentCellDirtyStateChanged+=(_,_)=>{if(grid.IsCurrentCellDirty)grid.CommitEdit(DataGridViewDataErrorContexts.Commit);};preview.Click+=(_,_)=>ReviewFile();excludeFile.Click+=(_,_)=>ExcludeChosen(false);excludeFolder.Click+=(_,_)=>ExcludeChosen(true);manageExclusions.Click+=(_,_)=>ManageExclusions();
        grid.CellDoubleClick+=(_,e)=>{if(!busy&&e.RowIndex>=0&&e.ColumnIndex!=0&&grid.Rows[e.RowIndex].DataBoundItem is DuplicateItem item)OpenItem(item);};grid.ColumnHeaderMouseClick+=(_,e)=>SortBy(e.ColumnIndex);pathBox.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter)await ScanAsync();};DragEnter+=(_,e)=>{if(e.Data?.GetDataPresent(DataFormats.FileDrop)==true)e.Effect=DragDropEffects.Copy;};DragDrop+=(_,e)=>AcceptDrop(e);FormClosing+=(_,e)=>{if(busy){e.Cancel=true;cts?.Cancel();status.Text="Cancelling operation • close again when it finishes";}else SavePreferences();};}
    void ChooseFolder(){using var d=new FolderBrowserDialog{Description="Select a folder or drive to scan",ShowNewFolderButton=false};if(d.ShowDialog()==DialogResult.OK)pathBox.Text=d.SelectedPath;}
    async Task ScanAsync(){if(busy)return;var root=pathBox.Text.Trim();if(!Directory.Exists(root)){MessageBox.Show("Choose an existing folder or drive.","DedupeLight",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}cts=new();SetBusy(true);allItems=[];ApplyFilter();var reporter=new Progress<string>(s=>{if(busy)status.Text=s;});try{var result=await DuplicateScanner.FindAsync(root,CachePath,new ScanOptions((long)minimumSize.Value*1024*1024,hiddenSystem.Checked,exclusions.ToArray()),reporter,cts.Token);hasScanned=true;allItems=result.Items;ApplyFilter();var optimization=result.SizeCandidates-result.FullHashes-result.CacheHits;status.Text=$"Scanned {result.FilesSeen:N0} files • {result.FullHashes:N0} full reads • {result.CacheHits:N0} cached • {Math.Max(0,optimization):N0} rejected by sampling"+(result.Skipped>0?$" • {result.Skipped:N0} skipped":"");}catch(OperationCanceledException){status.Text="Scan cancelled";}catch(Exception ex){status.Text="Scan failed";MessageBox.Show(ex.Message,"Could not scan",MessageBoxButtons.OK,MessageBoxIcon.Error);}finally{cts?.Dispose();cts=null;SetBusy(false);UpdateSummary();}}
    void ApplyFilter(){var q=searchBox.Text.Trim();IEnumerable<DuplicateItem> shown=string.IsNullOrEmpty(q)?allItems:allItems.Where(x=>x.Name.Contains(q,StringComparison.OrdinalIgnoreCase)||x.Folder.Contains(q,StringComparison.OrdinalIgnoreCase));shown=sortColumn switch{1=>sortAscending?shown.OrderBy(x=>x.Group):shown.OrderByDescending(x=>x.Group),2=>sortAscending?shown.OrderBy(x=>x.Name):shown.OrderByDescending(x=>x.Name),3=>sortAscending?shown.OrderBy(x=>x.Folder):shown.OrderByDescending(x=>x.Folder),4=>sortAscending?shown.OrderBy(x=>x.Size):shown.OrderByDescending(x=>x.Size),5=>sortAscending?shown.OrderBy(x=>x.ModifiedUtc):shown.OrderByDescending(x=>x.ModifiedUtc),_=>shown.OrderBy(x=>x.Group).ThenBy(x=>x.Path)};rebinding=true;try{source.DataSource=shown.ToList();}finally{rebinding=false;}UpdateEmptyState();UpdateSummary();UpdateActions();}
    void UpdateEmptyState(){emptyState.Visible=source.Count==0;grid.Visible=!emptyState.Visible;emptyState.Text=busy?"Finding exact copies…\n\nYour files stay untouched. Use Cancel to stop the scan.":allItems.Count>0?"No matches.\n\nTry another file name or location.":hasScanned?"All clear.\n\nNo exact duplicates found with these scan settings.":"A little more room for what matters.\n\nChoose a folder or drop one here to find exact duplicates.";}
    void UpdateSummary(){var groups=allItems.Select(x=>x.Group).Distinct().Count();var reclaim=allItems.GroupBy(x=>x.Hash).Sum(g=>g.First().Size*(g.Count()-1));summary.Text=groups==0?(cts is null?"No duplicates to review":"Scanning…"):$"{groups:N0} duplicate sets  •  {allItems.Count:N0} files  •  {FormatSize(reclaim)} recoverable";}
    void SetBusy(bool busy){this.busy=busy;manageExclusions.Enabled=!busy;grid.Enabled=searchBox.Enabled=!busy;pathBox.Enabled=browse.Enabled=scan.Enabled=minimumSize.Enabled=hiddenSystem.Enabled=keepRule.Enabled=!busy;cancel.Visible=cancel.Enabled=busy&&cts is not null;progress.Visible=busy;UpdateEmptyState();UpdateActions();}
    void UpdateActions(){if(rebinding)return;var available=allItems.Count>0&&!busy;var marked=allItems.Where(x=>x.Selected).ToList();var chosen=marked.Count>0||grid.SelectedRows.Count>0;selectExtras.Enabled=clearSelection.Enabled=export.Enabled=available;recycle.Enabled=move.Enabled=copy.Enabled=available&&chosen;preview.Enabled=open.Enabled=reveal.Enabled=available&&grid.SelectedRows.Count==1;excludeFile.Enabled=excludeFolder.Enabled=available&&chosen;
        var current=Current();fileDetails.Text=current is null?"Select a row to review · double-click to open":$"{current.Name}  ·  {current.SizeText}  ·  {current.Path}";
        hints.SetToolTip(fileDetails,current?.Path??"");
        manageExclusions.Text=$"Excluded sources ({exclusions.Count})";selectedSummary.Text=marked.Count==0?"":$"{marked.Count:N0} selected • {FormatSize(marked.Sum(x=>x.Size))}";}
    List<DuplicateItem> Chosen(){grid.EndEdit();var marked=allItems.Where(x=>x.Selected).ToList();return marked.Count>0?marked:grid.SelectedRows.Cast<DataGridViewRow>().Select(r=>r.DataBoundItem as DuplicateItem).Where(x=>x is not null).Cast<DuplicateItem>().Distinct().ToList();}DuplicateItem? Current()=>grid.SelectedRows.Count==1?grid.SelectedRows[0].DataBoundItem as DuplicateItem:null;
    void SelectExtras(){allItems.ForEach(x=>x.Selected=false);foreach(var group in allItems.GroupBy(x=>x.Hash)){IOrderedEnumerable<DuplicateItem> ordered=keepRule.SelectedIndex switch{1=>group.OrderByDescending(x=>x.ModifiedUtc).ThenBy(x=>x.Path),2=>group.OrderBy(x=>x.Path.Length).ThenBy(x=>x.Path),_=>group.OrderBy(x=>x.ModifiedUtc).ThenBy(x=>x.Path)};foreach(var item in ordered.Skip(1))item.Selected=true;}grid.Refresh();UpdateActions();status.Text=$"Selected extra copies • {keepRule.SelectedItem?.ToString()?.ToLowerInvariant()} in every set";}
    async Task RecycleAsync(){if(busy)return;var chosen=Chosen();if(chosen.Count==0)return;var fullGroups=RecyclePolicy.FullySelectedGroups(allItems,chosen);if(fullGroups.Count>0){using var warning=new RecycleOverrideDialog(fullGroups,chosen);if(warning.ShowDialog(this)!=DialogResult.OK)return;}var bytes=chosen.Sum(x=>x.Size);if(MessageBox.Show($"Move {chosen.Count} file(s) ({FormatSize(bytes)}) to the Recycle Bin?\n\nYou can restore them later.","Confirm recycle",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;SetBusy(true);status.Text="Recycling files…";var failed=new List<string>();var recycled=new HashSet<DuplicateItem>();try{await Task.Run(()=>{foreach(var x in chosen)try{FileSystem.DeleteFile(x.Path,UIOption.OnlyErrorDialogs,RecycleOption.SendToRecycleBin);if(!File.Exists(x.Path))recycled.Add(x);else failed.Add(x.Path);}catch{failed.Add(x.Path);}});allItems.RemoveAll(x=>recycled.Contains(x));RemoveOrphans();ApplyFilter();status.Text=failed.Count==0?$"Recycled {chosen.Count} file(s) • {FormatSize(bytes)} reclaimed":$"Finished • {failed.Count} file(s) could not be recycled";}finally{SetBusy(false);}}
    async Task TransferAsync(bool moving)
    {
        if(busy)return;
        var chosen=Chosen();if(chosen.Count==0)return;
        using var d=new FolderBrowserDialog{Description=moving?"Move selected files to":"Copy selected files to"};
        if(d.ShowDialog()!=DialogResult.OK)return;
        var destination=d.SelectedPath;var succeeded=new HashSet<DuplicateItem>();var failed=0;
        SetBusy(true);status.Text=moving?"Moving files…":"Copying files…";
        try
        {
            await Task.Run(()=>{foreach(var x in chosen)try{
                if(string.Equals(Path.GetFullPath(x.Folder),Path.GetFullPath(destination),StringComparison.OrdinalIgnoreCase))throw new IOException("Source and destination are the same folder.");
                var target=UniquePath(destination,x.Name);
                if(moving)File.Move(x.Path,target);else File.Copy(x.Path,target);
                succeeded.Add(x);
            }catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){failed++;}});
            if(moving){allItems.RemoveAll(x=>succeeded.Contains(x));RemoveOrphans();ApplyFilter();}
            status.Text=$"{(moving?"Moved":"Copied")} {succeeded.Count} file(s)"+(failed>0?$" • {failed} skipped":"");
        }
        catch(Exception ex){MessageBox.Show(ex.Message,"File operation failed");}
        finally{SetBusy(false);}
    }
    void OpenFile(){if(!busy&&Current() is {} item)OpenItem(item);}
    void OpenItem(DuplicateItem item){try{Process.Start(new ProcessStartInfo(item.Path){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(ex.Message,"Could not open file");}}void Reveal(){var x=Current();if(x is null)return;Process.Start(new ProcessStartInfo("explorer.exe",$"/select,\"{x.Path}\""){UseShellExecute=true});}
    async Task ExportCsvAsync()
    {
        if(busy)return;
        using var d=new SaveFileDialog{Filter="CSV file (*.csv)|*.csv",FileName="duplicate-files.csv"};
        if(d.ShowDialog()!=DialogResult.OK)return;
        var destination=d.FileName;var items=allItems.ToArray();SetBusy(true);status.Text="Exporting results…";
        try
        {
            await Task.Run(()=>{
                using var writer=new StreamWriter(destination,false,new UTF8Encoding(true));
                writer.WriteLine("Set,File,Folder,Bytes,Modified,SHA256");
                foreach(var x in items)writer.WriteLine($"{x.Group},{Csv(x.Name)},{Csv(x.Folder)},{x.Size},{x.ModifiedUtc:O},{x.Hash}");
            });
            status.Text=$"Exported {items.Length:N0} results";
        }
        catch(Exception ex){MessageBox.Show(ex.Message,"Could not export");status.Text="Export failed";}
        finally{SetBusy(false);}
    }
    void SortBy(int column){if(column==0)return;if(sortColumn==column)sortAscending=!sortAscending;else{sortColumn=column;sortAscending=true;}foreach(DataGridViewColumn item in grid.Columns)item.HeaderCell.SortGlyphDirection=SortOrder.None;grid.Columns[column].HeaderCell.SortGlyphDirection=sortAscending?SortOrder.Ascending:SortOrder.Descending;ApplyFilter();}
    void AcceptDrop(DragEventArgs e){if(busy)return;if(e.Data?.GetData(DataFormats.FileDrop) is not string[] paths||paths.Length==0)return;var path=paths[0];if(File.Exists(path))path=Path.GetDirectoryName(path)??path;if(Directory.Exists(path)){pathBox.Text=path;status.Text="Folder ready • select Scan now";}}
    void LoadPreferences(){var settings=AppSettings.Load(SettingsPath);try{exclusions=ExclusionRules.Validate(settings.Exclusions??[]);}catch{exclusions=[];}if(Directory.Exists(settings.LastPath))pathBox.Text=settings.LastPath;minimumSize.Value=Math.Clamp(settings.MinimumMegabytes,minimumSize.Minimum,minimumSize.Maximum);hiddenSystem.Checked=settings.IncludeHiddenSystem;keepRule.SelectedIndex=Math.Clamp(settings.KeepRule,0,keepRule.Items.Count-1);}
    void SavePreferences()=>new AppSettings(pathBox.Text.Trim(),minimumSize.Value,hiddenSystem.Checked,keepRule.SelectedIndex,exclusions).Save(SettingsPath);
    void ReviewFile()
    {
        if(busy||Current() is not {} item)return;
        using var review=new FileReviewDialog(item);
        if(review.ShowDialog(this)==DialogResult.OK&&review.ExcludeFolder is bool folder)AddExclusions([item],folder);
    }
    void ExcludeChosen(bool folder){if(!busy)AddExclusions(Chosen(),folder);}
    void AddExclusions(IReadOnlyList<DuplicateItem> items,bool folder)
    {
        if(items.Count==0)return;
        var rules=ExclusionRules.Validate(items.Select(x=>new ExclusionRule(folder?x.Folder:x.Path,folder)));
        if(MessageBox.Show(this,$"Exclude {rules.Count} {(folder?"folder(s), including their subfolders":"file(s)")} from current results and future scans?\n\nFiles remain on disk. Restore them through Excluded sources.", "Exclude intentional duplicates", MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
        exclusions=ExclusionRules.Validate(exclusions.Concat(rules));SavePreferences();ApplyExclusions();
        status.Text="Sources excluded · files remain untouched";
    }
    void ApplyExclusions(){allItems.RemoveAll(x=>ExclusionRules.Matches(x.Path,exclusions));RemoveOrphans();ApplyFilter();}
    void ManageExclusions()
    {
        if(busy)return;using var dialog=new ExclusionDialog(exclusions);
        if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        exclusions=dialog.Rules;SavePreferences();ApplyExclusions();
        status.Text="Exclusions updated · scan again to restore sources removed from the exclusion list";
    }
    static string Csv(string value)=>$"\"{value.Replace("\"","\"\"")}\"";static string UniquePath(string folder,string name){var p=Path.Combine(folder,name);if(!File.Exists(p))return p;var stem=Path.GetFileNameWithoutExtension(name);var ext=Path.GetExtension(name);for(var i=2;;i++){p=Path.Combine(folder,$"{stem} ({i}){ext}");if(!File.Exists(p))return p;}}void RemoveOrphans(){allItems=allItems.GroupBy(x=>x.Hash).Where(g=>g.Count()>1).SelectMany(g=>g).ToList();}
    protected override void Dispose(bool disposing){if(disposing){filterDelay.Dispose();hints.Dispose();source.Dispose();cts?.Dispose();}base.Dispose(disposing);}
    public static string FormatSize(long bytes){string[] units=["B","KB","MB","GB","TB"];double value=bytes;var unit=0;while(value>=1024&&unit<units.Length-1){value/=1024;unit++;}return $"{value:0.##} {units[unit]}";}
}
