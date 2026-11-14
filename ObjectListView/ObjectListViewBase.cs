/*
 * ObjectListViewBase - Shared listview implementation for collections of objects
 *
 * Author: Phillip Piper
 * Date: 9/10/2006 11:15 AM
 *
 * Change log
 * 2018-10-06  JPP  - InsertObjects() when in a non-Detail View now correctly positions the items (SF bug #154)
 * 2018-09-01  JPP  - Hardened code against the rare case of the control having no columns (SF bug #174)
 *                    The underlying ListView does not like having rows when there are no columns and throws exceptions.j
 * 2018-05-05  JPP  - Added OLVColumn.EditorCreator to allow fine control over what control is used to edit
 *                    a particular cell.
 *                  - Added IOlvEditor to allow custom editor to easily integrate with our editing scheme
 *                  - ComboBoxes resize drop downs to show the widest entry via ControlUtilities.AutoResizeDropDown()
 * 2018-05-03  JPP  - Extend OnColumnRightClick so the event handler can tweak the menu to be shown
 * 2018-04-27  JPP  - Sorting now works when grouping is locked on a column AND SortGroupItemsByPrimaryColumn is true
 *                  - Correctly report right clicks on group headers via CellRightClick events.
 * v2.9.2
 * 2016-06-02  JPP  - Cell editors now respond to mouse wheel events. Set AllowCellEditorsToProcessMouseWheel
 *                    to false revert to previous behaviour.
 *                  - Fixed issue in PauseAnimations() that prevented it from working until
 *                    after the control had been rendered at least once.
 *                  - CellEditUseWholeCell now has correct default value (true).
 *                  - Dropping on a subitem when CellEditActivation is set to SingleClick no longer 
 *                    initiates a cell edit
 * v2.9.1
 * 2015-12-30  JPP  - Added CellRendererGetter to allow each cell to have a different renderer.
 *                  - Obsolete properties are no longer code-gen'ed.
 *
 * v2.9.0
 * 2015-08-22  JPP  - Allow selected row back/fore colours to be specified for each row
 *                  - Renamed properties related to selection colours:
 *                       - HighlightBackgroundColor -> SelectedBackColor
 *                       - HighlightForegroundColor -> SelectedForeColor
 *                       - UnfocusedHighlightBackgroundColor -> UnfocusedSelectedBackColor
 *                       - UnfocusedHighlightForegroundColor -> UnfocusedSelectedForeColor
 *                  - UseCustomSelectionColors is no longer used
 * 2015-08-03  JPP  - Added ObjectListView.CellEditFinished event
 *                  - Added EditorRegistry.Unregister()
 * 2015-07-08  JPP  - All ObjectListViews are now OwnerDrawn by default. This allows all the great features
 *                    of ObjectListView to work correctly at the slight cost of more processing at render time.
 *                    It also avoids the annoying "hot item background ignored in column 0" behaviour that native
 *                    ListView has. Programmers can still turn it back off if they wish.
 * 2015-06-27  JPP  - Yet another attempt to disable ListView's "shift click toggles checkboxes" behaviour.
 *                    The last strategy (fake right click) worked, but had nasty side effects. This one works
 *                    by intercepting a HITTEST message so that it fails. It no longer creates fake right mouse events.
 *                  - Trigger SelectionChanged when filter is changed
 * 2015-06-23  JPP  - [BIG] Added support for Buttons
 * 2015-06-22  JPP  - Added OLVColumn.SearchValueGetter to allow the text used when text filtering to be customised
 *                  - The default DefaultRenderer is now a HighlightTextRenderer, since that seems more generally useful
 * 2015-06-17  JPP  - Added FocusedObject property
 *                  - Hot item is now always applied to the row even if FullRowSelect is false
 * 2015-06-11  JPP  - Added DefaultHotItemStyle property
 * 2015-06-07  JPP  - Added HeaderMinimumHeight property
 *                  - Added ObjectListView.CellEditUsesWholeCell and OLVColumn.CellEditUsesWholeCell properties.
 * 2015-05-15  JPP  - Allow ImageGetter to return an Image (which I can't believe didn't work from the beginning!)
 * 2015-04-27  JPP  - Fix bug where setting View to LargeIcon in the designer was not persisted
 * 2015-04-07  JPP  - Ensure changes to row.Font in FormatRow are not wiped out by FormatCell (SF #141)
 * 
 * v2.8.1
 * 2014-10-15  JPP  - Added CellEditActivateMode.SingleClickAlways mode
 *                  - Fire Filter event even if ModelFilter and ListFilter are null (SF #126)
 *                  - Fixed issue where single-click editing didn't work (SF #128)
 * v2.8.0
 * 2014-10-11  JPP  - Fixed some XP-only flicker issues
 * 2014-09-26  JPP  - Fixed intricate bug involving checkboxes on non-owner-drawn virtual lists.
 *                  - Fixed long standing (but previously unreported) error on non-details virtual lists where
 *                    users could not click on checkboxes.
 * 2014-09-07  JPP  - (Major) Added ability to have checkboxes in headers
 *                  - CellOver events are raised when the mouse moves over the header. Set TriggerCellOverEventsWhenOverHeader
 *                    to false to disable this behaviour.
 *                  - Freeze/Unfreeze now use BeginUpdate/EndUpdate to disable Window level drawing
 *                  - Changed default value of ObjectListView.HeaderUsesThemes from true to false. Too many people were
 *                    being confused, trying to make something interesting appear in the header and nothing showing up
 * 2014-08-04  JPP  - Final attempt to fix the multiple hyperlink events being raised. This involves turning
 *                    a NM_CLICK notification into a NM_RCLICK.
 * 2014-05-21  JPP  - (Major) Added ability to disable rows. DisabledObjects, DisableObjects(), DisabledItemStyle.
 * 2014-04-25  JPP  - Fixed issue where virtual lists containing a single row didn't update hyperlinks on MouseOver
 *                  - Added sanity check before BuildGroups()
 * 2014-03-22  JPP  - Fixed some subtle bugs resulting from misuse of TryGetValue()
 * 2014-03-09  JPP  - Added CollapsedGroups property
 *                  - Several minor Resharper complaints quiesced.
 * v2.7
 * 2014-02-14  JPP  - Fixed issue with ShowHeaderInAllViews (another one!) where setting it to false caused the list to lose
 *                    its other extended styles, leading to nasty flickering and worse.
 * 2014-02-06  JPP  - Fix issue on virtual lists where the filter was not correctly reapplied after columns were added or removed.
 *                  - Made disposing of cell editors optional (defaults to true). This allows controls to be cached and reused.
 *                  - Bracketed column resizing with BeginUpdate/EndUpdate to smooth redraws (thanks to Davide)
 * 2014-02-01  JPP  - Added static property ObjectListView.GroupTitleDefault to allow the default group title to be localised.
 * 2013-09-24  JPP  - Fixed issue in RefreshObjects() when model objects overrode the Equals()/GetHashCode() methods.
 *                  - Made sure get state checker were used when they should have been
 * 2013-04-21  JPP  - Clicking on a non-groupable column header when showing groups will now sort
 *                    the group contents by that column.
 * v2.6
 * 2012-08-16  JPP  - Added ObjectListView.EditModel() -- a convenience method to start an edit operation on a model
 * 2012-08-10  JPP  - Don't trigger selection changed events during sorting/grouping or add/removing columns
 * 2012-08-06  JPP  - Don't start a cell edit operation when the user clicks on the background of a checkbox cell.
 *                  - Honor values from the BeforeSorting event when calling a CustomSorter
 * 2012-08-02  JPP  - Added CellVerticalAlignment and CellPadding properties.
 * 2012-07-04  JPP  - Fixed issue with cell editing where the cell editing didn't finish until the first idle event.
 *                    This meant that if you clicked and held on the scroll thumb to finish a cell edit, the editor
 *                    wouldn't be removed until the mouse was released.
 * 2012-07-03  JPP  - Fixed issue with SingleClick cell edit mode where the cell editing would not begin until the
 *                    mouse moved after the click.
 * 2012-06-25  JPP  - Fixed bug where removing a column from a LargeIcon or SmallIcon view would crash the control. 
 * 2012-06-15  JPP  - Added Reset() method, which definitively removes all rows *and* columns from an ObjectListView.
 * 2012-06-11  JPP  - Added FilteredObjects property which returns the collection of objects that survives any installed filters.
 * 2012-06-04  JPP  - [Big] Added UseNotifyPropertyChanged to allow OLV to listen for INotifyPropertyChanged events on models.
 * 2012-05-30  JPP  - Added static property ObjectListView.IgnoreMissingAspects. If this is set to true, all 
 *                    ObjectListViews will silently ignore missing aspect errors. Read the remarks to see why this would be useful.
 * 2012-05-23  JPP  - Setting UseFilterIndicator to true now sets HeaderUsesTheme to false. 
 *                    Also, changed default value of UseFilterIndicator to false. Previously, HeaderUsesTheme and UseFilterIndicator
 *                    defaulted to true, which was pointless since when the HeaderUsesTheme is true, UseFilterIndicator does nothing.  
 * v2.5.1
 * 2012-05-06  JPP  - Fix bug where collapsing the first group would cause decorations to stop being drawn (SR #3502608)
 * 2012-04-23  JPP  - Trigger GroupExpandingCollapsing event to allow the expand/collapse to be cancelled
 *                  - Fixed SetGroupSpacing() so it corrects updates the space between all groups.
 *                  - ResizeLastGroup() now does nothing since it was broken and I can't remember what it was
 *                    even supposed to do :)
 * 2012-04-18  JPP  - Upgraded hit testing to include hits on groups. 
 *                  - HotItemChanged is now correctly recalculated on each mouse move. Includes "hot" group information.
 * 2012-04-14  JPP  - Added GroupStateChanged event. Useful for knowing when a group is collapsed/expanded.
 *                  - Added AdditionalFilter property. This filter is combined with the Excel-like filtering that
 *                    the end user might enact at runtime.
 * 2012-04-10  JPP  - Added PersistentCheckBoxes property to allow primary checkboxes to remember their values
 *                    across list rebuilds.
 * 2012-04-05  JPP  - Reverted some code to .NET 2.0 standard.
 *                  - Tweaked some code
 * 2012-02-05  JPP  - Fixed bug when selecting a separator on a drop down menu
 * 2011-06-24  JPP  - Added CanUseApplicationIdle property to cover cases where Application.Idle events
 *                    are not triggered. For example, when used within VS (and probably Office) extensions
 *                    Application.Idle is never triggered. Set CanUseApplicationIdle to false to handle 
 *                    these cases.
 *                  - Handle cases where a second tool tip is installed onto the ObjectListView.
 *                  - Correctly recolour rows after an Insert or Move
 *                  - Removed m.LParam cast which could cause overflow issues on Win7/64 bit.
 * v2.5.0
 * 2011-05-31  JPP  - SelectObject() and SelectObjects() no longer deselect all other rows.
                      Set the SelectedObject or SelectedObjects property to do that.
 *                  - Added CheckedObjectsEnumerable
 *                  - Made setting CheckedObjects more efficient on large collections
 *                  - Deprecated GetSelectedObject() and GetSelectedObjects()
 * 2011-04-25  JPP  - Added SubItemChecking event
 *                  - Fixed bug in handling of NewValue on CellEditFinishing event
 * 2011-04-12  JPP  - Added UseFilterIndicator 
 *                  - Added some more localizable messages
 * 2011-04-10  JPP  - FormatCellEventArgs now has a CellValue property, which is the model value displayed
 *                    by the cell. For example, for the Birthday column, the CellValue might be 
 *                    DateTime(1980, 12, 31), whereas the cell's text might be 'Dec 31, 1980'.
 * 2011-04-04  JPP  - Tweaked UseTranslucentSelection and UseTranslucentHotItem to look (a little) more
 *                    like Vista/Win7.
 *                  - Alternate colours are now only applied in Details view (as they always should have been)
 *                  - Alternate colours are now correctly recalculated after removing objects
 * 2011-03-29  JPP  - Added SelectColumnsOnRightClickBehaviour to allow the selecting of columns mechanism 
 *                    to be changed. Can now be InlineMenu (the default), SubMenu, or ModelDialog.
 *                  - ColumnSelectionForm was moved from the demo into the ObjectListView project itself.
 *                  - Ctrl-C copying is now able to use the DragSource to create the data transfer object.
 * 2011-03-19  JPP  - All model object comparisons now use Equals rather than == (thanks to vulkanino)
 *                  - [Small Break] GetNextItem() and GetPreviousItem() now accept and return OLVListView
 *                    rather than ListViewItems.
 * 2011-03-07  JPP  - [Big] Added Excel-style filtering. Right click on a header to show a Filtering menu.
 *                  - Added CellEditKeyEngine to allow key handling when cell editing to be completely customised.
 *                    Add CellEditTabChangesRows and CellEditEnterChangesRows to show some of these abilities.
 * 2011-03-06  JPP  - Added OLVColumn.AutoCompleteEditorMode in preference to AutoCompleteEditor 
 *                    (which is now just a wrapper). Thanks to Clive Haskins 
 *                  - Added lots of docs to new classes
 * 2011-02-25  JPP  - Preserve word wrap settings on TreeListView
 *                  - Resize last group to keep it on screen (thanks to ?)
 * 2010-11-16  JPP  - Fixed (once and for all) DisplayIndex problem with Generator
 *                  - Changed the serializer used in SaveState()/RestoreState() so that it resolves on
 *                    class name alone.
 *                  - Fixed bug in GroupWithItemCountSingularFormatOrDefault
 *                  - Fixed strange flickering in grouped, owner drawn OLV's using RefreshObject()
 * v2.4.1
 * 2010-08-25  JPP  - Fixed bug where setting OLVColumn.CheckBoxes to false gave it a renderer
 *                    specialized for checkboxes. Oddly, this made Generator created owner drawn
 *                    lists appear to be completely empty.
 *                  - In IDE, all ObjectListView properties are now in a single "ObjectListView" category,
 *                    rather than splitting them between "Appearance" and "Behavior" categories.
 *                  - Added GroupingParameters.GroupComparer to allow groups to be sorted in a customizable fashion.
 *                  - Sorting of items within a group can be disabled by setting 
 *                    GroupingParameters.PrimarySortOrder to None.
 * 2010-08-24  JPP  - Added OLVColumn.IsHeaderVertical to make a column draw its header vertical.
 *                  - Added OLVColumn.HeaderTextAlign to control the alignment of a column's header text.
 *                  - Added HeaderMaximumHeight to limit how tall the header section can become
 * 2010-08-18  JPP  - Fixed long standing bug where having 0 columns caused a InvalidCast exception.
 *                  - Added IncludeAllColumnsInDataObject property
 *                  - Improved BuildList(bool) so that it preserves scroll position even when
 *                    the listview is grouped.
 * 2010-08-08  JPP  - Added OLVColumn.HeaderImageKey to allow column headers to have an image.
 *                  - CellEdit validation and finish events now have NewValue property.
 * 2010-08-03  JPP  - Subitem checkboxes improvements: obey IsEditable, can be hot, can be disabled.
 *                  - No more flickering of selection when tabbing between cells
 *                  - Added EditingCellBorderDecoration to make it clearer which cell is being edited.
 * 2010-08-01  JPP  - Added ObjectListView.SmoothingMode to control the smoothing of all graphics
 *                    operations
 *                  - Columns now cache their group item format strings so that they still work as 
 *                    grouping columns after they have been removed from the listview. This cached
 *                    value is only used when the column is not part of the listview.
 * 2010-07-25  JPP  - Correctly trigger a Click event when the mouse is clicked.
 * 2010-07-16  JPP  - Invalidate the control before and after cell editing to make sure it looks right
 * 2010-06-23  JPP  - Right mouse clicks on checkboxes no longer confuse them
 * 2010-06-21  JPP  - Avoid bug in underlying ListView control where virtual lists in SmallIcon view
 *                    generate GETTOOLINFO msgs with invalid item indices.
 *                  - Fixed bug where FastObjectListView would throw an exception when showing hyperlinks
 *                    in any view except Details.
 * 2010-06-15  JPP  - Fixed bug in ChangeToFilteredColumns() that resulted in column display order
 *                    being lost when a column was hidden.
 *                  - Renamed IsVista property to IsVistaOrLater which more accurately describes its function.
 * v2.4
 * 2010-04-14  JPP  - Prevent object disposed errors when mouse event handlers cause the
 *                    ObjectListView to be destroyed (e.g. closing a form during a 
 *                    double click event).
 *                  - Avoid checkbox munging bug in standard ListView when shift clicking on non-primary
 *                    columns when FullRowSelect is true.
 * 2010-04-12  JPP  - Fixed bug in group sorting (thanks Mike).
 * 2010-04-07  JPP  - Prevent hyperlink processing from triggering spurious MouseUp events.
 *                    This showed itself by launching the same url multiple times.
 * 2010-04-06  JPP  - Space filling columns correctly resize upon initial display
 *                  - ShowHeaderInAllViews is better but still not working reliably.
 *                    See comments on property for more details.
 * 2010-03-23  JPP  - Added ObjectListView.HeaderFormatStyle and OLVColumn.HeaderFormatStyle.
 *                    This makes HeaderFont and HeaderForeColor properties unnecessary -- 
 *                    they will be marked obsolete in the next version and removed after that.
 * 2010-03-16  JPP  - Changed object checking so that objects can be pre-checked before they
 *                    are added to the list. Normal ObjectListViews managed "checkedness" in
 *                    the ListViewItem, so this won't work for them, unless check state getters
 *                    and putters have been installed. It will work not on virtual lists (thus fast lists and
 *                    tree views) since they manage their own check state.
 * 2010-03-06  JPP  - Hide "Items" and "Groups" from the IDE properties grid since they shouldn't be set like that.
 *                    They can still be accessed through "Custom Commands" and there's nothing we can do
 *                    about that.
 * 2010-03-05  JPP  - Added filtering
 * 2010-01-18  JPP  - Overlays can be turned off. They also only work on 32-bit displays
 * v2.3
 * 2009-10-30  JPP  - Plugged possible resource leak by using using() with CreateGraphics()
 * 2009-10-28  JPP  - Fix bug when right clicking in the empty area of the header
 * 2009-10-20  JPP  - Redraw the control after setting EmptyListMsg property
 * v2.3
 * 2009-09-30  JPP  - Added Dispose() method to properly release resources
 * 2009-09-16  JPP  - Added OwnerDrawnHeader, which you can set to true if you want to owner draw
 *                    the header yourself.
 * 2009-09-15  JPP  - Added UseExplorerTheme, which allow complete visual compliance with Vista explorer.
 *                    But see property documentation for its many limitations.
 *                  - Added ShowHeaderInAllViews. To make this work, Columns are no longer
 *                    changed when switching to/from Tile view.
 * 2009-09-11  JPP  - Added OLVColumn.AutoCompleteEditor to allow the autocomplete of cell editors
 *                    to be disabled.
 * 2009-09-01  JPP  - Added ObjectListView.TextRenderingHint property which controls the
 *                    text rendering hint of all drawn text.
 * 2009-08-28  JPP  - [BIG] Added group formatting to supercharge what is possible with groups
 *                  - [BIG] Virtual groups now work
 *                  - Extended MakeGroupies() to handle more aspects of group creation
 * 2009-08-19  JPP  - Added ability to show basic column commands when header is right clicked
 *                  - Added SelectedRowDecoration, UseTranslucentSelection and UseTranslucentHotItem.
 *                  - Added PrimarySortColumn and PrimarySortOrder
 * 2009-08-15  JPP  - Correct problems with standard hit test and subitems
 * 2009-08-14  JPP  - [BIG] Support Decorations
 *                  - [BIG] Added header formatting capabilities: font, color, word wrap
 *                  - Gave ObjectListView its own designer to hide unwanted properties
 *                  - Separated design time stuff into separate file
 *                  - Added FormatRow and FormatCell events
 * 2009-08-09  JPP  - Get around bug in HitTest when not FullRowSelect
 *                  - Added OLVListItem.GetSubItemBounds() method which works correctly
 *                    for all columns including column 0
 * 2009-08-07  JPP  - Added Hot* properties that track where the mouse is
 *                  - Added HotItemChanged event
 *                  - Overrode TextAlign on columns so that column 0 can have something other
 *                    than just left alignment. This is only honored when owner drawn.
 * v2.2.1
 * 2009-08-03  JPP  - Subitem edit rectangles always allowed for an image in the cell, even if there was none.
 *                    Now they only allow for an image when there actually is one.
 *                  - Added Bounds property to OLVListItem which handles items being part of collapsed groups.
 * 2009-07-29  JPP  - Added GetSubItem() methods to ObjectListView and OLVListItem
 * 2009-07-26  JPP  - Avoided bug in .NET framework involving column 0 of owner drawn listviews not being
 *                    redrawn when the listview was scrolled horizontally (this was a LOT of work to track
 *                    down and fix!)
 *                  - The cell edit rectangle is now correctly calculated when the listview is scrolled
 *                    horizontally.
 * 2009-07-14  JPP  - If the user clicks/double clicks on a tree list cell, an edit operation will no longer begin
 *                    if the click was to the left of the expander. This is implemented in such a way that
 *                    other renderers can have similar "dead" zones.
 * 2009-07-11  JPP  - CalculateCellBounds() messed with the FullRowSelect property, which confused the
 *                    tooltip handling on the underlying control. It no longer does this.
 *                  - The cell edit rectangle is now correctly calculated for owner-drawn, non-Details views.
 * 2009-07-08  JPP  - Added Cell events (CellClicked, CellOver, CellRightClicked)
 *                  - Made BuildList(), AddObject() and RemoveObject() thread-safe
 * 2009-07-04  JPP  - Space bar now properly toggles checkedness of selected rows
 * 2009-07-02  JPP  - Fixed bug with tooltips when the underlying Windows control was destroyed.
 *                  - CellToolTipShowing events are now triggered in all views.
 * v2.2
 * 2009-06-02  JPP  - BeforeSortingEventArgs now has a Handled property to let event handlers do
 *                    the item sorting themselves.
 *                  - AlwaysGroupByColumn works again, as does SortGroupItemsByPrimaryColumn and all their
 *                    various permutations.
 *                  - SecondarySortOrder and SecondarySortColumn are now "null" by default
 * 2009-05-15  JPP  - Fixed bug so that KeyPress events are again triggered
 * 2009-05-10  JPP  - Removed all unsafe code
 * 2009-05-07  JPP  - Don't use glass panel for overlays when in design mode. It's too confusing.
 * 2009-05-05  JPP  - Added Scroll event (thanks to Christophe Hosten for the complete patch to implement this)
 *                  - Added Unfocused foreground and background colors (also thanks to Christophe Hosten)
 * 2009-04-29  JPP  - Added SelectedColumn property, which puts a slight tint on that column. Combine
 *                    this with TintSortColumn property and the sort column is automatically tinted.
 *                  - Use an overlay to implement "empty list" msg. Default empty list msg is now prettier.
 * 2009-04-28  JPP  - Fixed bug where DoubleClick events were not triggered when CheckBoxes was true
 * 2009-04-23  JPP  - Fixed various bugs under Vista.
 *                  - Made groups collapsible - Vista only. Thanks to Crustyapplesniffer.
 *                  - Forward events from DropSink to the control itself. This allows handlers to be defined
 *                    within the IDE for drop events
 * 2009-04-16  JPP  - Made several properties localizable.
 * 2009-04-11  JPP  - Correctly renderer checkboxes when RowHeight is non-standard
 * 2009-04-11  JPP  - Implemented overlay architecture, based on CustomDraw scheme.
 *                    This unified drag drop feedback, empty list msgs and overlay images.
 *                  - Added OverlayImage and friends, which allows an image to be drawn
 *                    transparently over the listview
 * 2009-04-10  JPP  - Fixed long-standing annoying flicker on owner drawn virtual lists!
 *                    This means, amongst other things, that grid lines no longer get confused,
 *                    and drag-select no longer flickers.
 * 2009-04-07  JPP  - Calculate edit rectangles more accurately
 * 2009-04-06  JPP  - Double-clicking no longer toggles the checkbox
 *                  - Double-clicking on a checkbox no longer confuses the checkbox
 * 2009-03-16  JPP  - Optimized the build of autocomplete lists
 * v2.1
 * 2009-02-24  JPP  - Fix bug where double-clicking VERY quickly on two different cells
 *                    could give two editors
 *                  - Maintain focused item when rebuilding list (SF #2547060)
 * 2009-02-22  JPP  - Reworked checkboxes so that events are triggered for virtual lists
 * 2009-02-15  JPP  - Added ObjectListView.ConfigureAutoComplete utility method
 * 2009-02-02  JPP  - Fixed bug with AlwaysGroupByColumn where column header clicks would not resort groups.
 * 2009-02-01  JPP  - OLVColumn.CheckBoxes and TriStateCheckBoxes now work.
 * 2009-01-28  JPP  - Complete overhaul of renderers!
 *                       - Use IRenderer
 *                       - Added ObjectListView.ItemRenderer to draw whole items
 * 2009-01-23  JPP  - Simple Checkboxes now work properly
 *                  - Added TriStateCheckBoxes property to control whether the user can
 *                    set the row checkbox to have the Indeterminate value
 *                  - CheckState property is now just a wrapper around the StateImageIndex property
 * 2009-01-20  JPP  - Changed to always draw columns when owner drawn, rather than falling back on DrawDefault.
 *                    This simplified several owner drawn problems
 *                  - Added DefaultRenderer property to help with the above
 *                  - HotItem background color is applied to all cells even when FullRowSelect is false
 *                  - Allow grouping by CheckedAspectName columns
 *                  - Commented out experimental animations. Still needs work.
 * 2009-01-17  JPP  - Added HotItemStyle and UseHotItem to highlight the row under the cursor
 *                  - Added UseCustomSelectionColors property
 *                  - Owner draw mode now honors ForeColor and BackColor settings on the list
 * 2009-01-16  JPP  - Changed to use EditorRegistry rather than hard coding cell editors
 * 2009-01-10  JPP  - Changed to use Equals() method rather than == to compare model objects.
 * v2.0.1
 * 2009-01-08  JPP  - Fixed long-standing "multiple columns generated" problem.
 *                    Thanks to pinkjones for his help with solving this one!
 *                  - Added EnsureGroupVisible()
 * 2009-01-07  JPP  - Made all public and protected methods virtual
 *                  - FinishCellEditing, PossibleFinishCellEditing and CancelCellEditing are now public
 * 2008-12-20  JPP  - Fixed bug with group comparisons when a group key was null (SF#2445761)
 * 2008-12-19  JPP  - Fixed bug with space filling columns and layout events
 *                  - Fixed RowHeight so that it only changes the row height, not the width of the images.
 * v2.0
 * 2008-12-10  JPP  - Handle Backspace key. Resets the search-by-typing state without delay
 *                  - Made some changes to the column collection editor to try and avoid
 *                    the multiple column generation problem.
 *                  - Updated some documentation
 * 2008-12-07  JPP  - Search-by-typing now works when showing groups
 *                  - Added BeforeSearching and AfterSearching events which are triggered when the user types
 *                    into the list.
 *                  - Added secondary sort information to Before/AfterSorting events
 *                  - Reorganized group sorting code. Now triggers Sorting events.
 *                  - Added GetItemIndexInDisplayOrder()
 *                  - Tweaked in the interaction of the column editor with the IDE so that we (normally)
 *                    don't rely on a hack to find the owning ObjectListView
 *                  - Changed all 'DefaultValue(typeof(Color), "Empty")' to 'DefaultValue(typeof(Color), "")'
 *                    since the first does not given Color.Empty as I thought, but the second does.
 * 2008-11-28  JPP  - Fixed long standing bug with horizontal scrollbar when shrinking the window.
 *                    (thanks to Bartosz Borowik)
 * 2008-11-25  JPP  - Added support for dynamic tooltips
 *                  - Split out comparers and header controls stuff into their own files
 * 2008-11-21  JPP  - Fixed bug where enabling grouping when there was not a sort column would not
 *                    produce a grouped list. Grouping column now defaults to column 0.
 *                  - Preserve selection on virtual lists when sorting
 * 2008-11-20  JPP  - Added ability to search by sort column to ObjectListView. Unified this with
 *                    ability that was already in VirtualObjectListView
 * 2008-11-19  JPP  - Fixed bug in ChangeToFilteredColumns() where DisplayOrder was not always restored correctly.
 * 2008-10-29  JPP  - Event argument blocks moved to directly within the namespace, rather than being
 *                    nested inside ObjectListView class.
 *                  - Removed OLVColumn.CellEditor since it was never used.
 *                  - Marked OLVColumn.AspectGetterAutoGenerated as obsolete (it has not been used for
 *                    several versions now).
 * 2008-10-28  JPP  - SelectedObjects is now an IList, rather than an ArrayList. This allows
 *                    it to accept generic list (e.g. List<File>).
 * 2008-10-09  JPP  - Support indeterminate checkbox values.
 *                    [BREAKING CHANGE] CheckStateGetter/CheckStatePutter now use CheckState types only.
 *                    BooleanCheckStateGetter and BooleanCheckStatePutter added to ease transition.
 * 2008-10-08  JPP  - Added setFocus parameter to SelectObject(), which allows focus to be set
 *                    at the same time as selecting.
 * 2008-09-27  JPP  - BIG CHANGE: Fissioned this file into separate files for each component
 * 2008-09-24  JPP  - Corrected bug with owner drawn lists where a column 0 with a renderer
 *                    would draw at column 0 even if column 0 was dragged to another position.
 *                  - Correctly handle space filling columns when columns are added/removed
 * 2008-09-16  JPP  - Consistently use try..finally for BeginUpdate()/EndUpdate() pairs
 * 2008-08-24  JPP  - If LastSortOrder is None when adding objects, don't force a resort.
 * 2008-08-22  JPP  - Catch and ignore some problems with setting TopIndex on FastObjectListViews.
 * 2008-08-05  JPP  - In the right-click column select menu, columns are now sorted by display order, rather than alphabetically
 * v1.13
 * 2008-07-23  JPP  - Consistently use copy-on-write semantics with Add/RemoveObject methods
 * 2008-07-10  JPP  - Enable validation on cell editors through a CellEditValidating event.
 *                    (thanks to Artiom Chilaru for the initial suggestion and implementation).
 * 2008-07-09  JPP  - Added HeaderControl.Handle to allow OLV to be used within UserControls.
 *                    (thanks to Michael Coffey for tracking this down).
 * 2008-06-23  JPP  - Split the more generally useful CopyObjectsToClipboard() method
 *                    out of CopySelectionToClipboard()
 * 2008-06-22  JPP  - Added AlwaysGroupByColumn and AlwaysGroupBySortOrder, which
 *                    force the list view to always be grouped by a particular column.
 * 2008-05-31  JPP  - Allow check boxes on FastObjectListViews
 *                  - Added CheckedObject and CheckedObjects properties
 * 2008-05-11  JPP  - Allow selection foreground and background colors to be changed.
 *                    Windows doesn't allow this, so we can only make it happen when owner
 *                    drawing. Set the HighlightForegroundColor and  HighlightBackgroundColor
 *                    properties and then call EnableCustomSelectionColors().
 * v1.12
 * 2008-05-08  JPP  - Fixed bug where the column select menu would not appear if the
 *                    ObjectListView has a context menu installed.
 * 2008-05-05  JPP  - Non detail views can now be owner drawn. The renderer installed for
 *                    primary column is given the chance to render the whole item.
 *                    See BusinessCardRenderer in the demo for an example.
 *                  - BREAKING CHANGE: RenderDelegate now returns a bool to indicate if default
 *                    rendering should be done. Previously returned void. Only important if your
 *                    code used RendererDelegate directly. Renderers derived from BaseRenderer
 *                    are unchanged.
 * 2008-05-03  JPP  - Changed cell editing to use values directly when the values are Strings.
 *                    Previously, values were always handed to the AspectToStringConverter.
 *                  - When editing a cell, tabbing no longer tries to edit the next subitem
 *                    when not in details view!
 * 2008-05-02  JPP  - MappedImageRenderer can now handle a Aspects that return a collection
 *                    of values. Each value will be drawn as its own image.
 *                  - Made AddObjects() and RemoveObjects() work for all flavours (or at least not crash)
 *                  - Fixed bug with clearing virtual lists that has been scrolled vertically
 *                  - Made TopItemIndex work with virtual lists.
 * 2008-05-01  JPP  - Added AddObjects() and RemoveObjects() to allow faster mods to the list
 *                  - Reorganised public properties. Now alphabetical.
 *                  - Made the class ObjectListViewState internal, as it always should have been.
 * v1.11
 * 2008-04-29  JPP  - Preserve scroll position when building the list or changing columns.
 *                  - Added TopItemIndex property. Due to problems with the underlying control, this
 *                    property is not always reliable. See property docs for info.
 * 2008-04-27  JPP  - Added SelectedIndex property.
 *                  - Use a different, more general strategy to handle Invoke(). Removed all delegates
 *                    that were only declared to support Invoke().
 *                  - Check all native structures for 64-bit correctness.
 * 2008-04-25  JPP  - Released on SourceForge.
 * 2008-04-13  JPP  - Added ColumnRightClick event.
 *                  - Made the assembly CLS-compliant. To do this, our cell editors were made internal, and
 *                    the constraint on FlagRenderer template parameter was removed (the type must still
 *                    be an IConvertible, but if it isn't, the error will be caught at runtime, not compile time).
 * 2008-04-12  JPP  - Changed HandleHeaderRightClick() to have a columnIndex parameter, which tells
 *                    exactly which column was right-clicked.
 * 2008-03-31  JPP  - Added SaveState() and RestoreState()
 *                  - When cell editing, scrolling with a mouse wheel now ends the edit operation.
 * v1.10
 * 2008-03-25  JPP  - Added space filling columns. See OLVColumn.FreeSpaceProportion property for details.
 *                    A space filling columns fills all (or a portion) of the width unoccupied by other columns.
 * 2008-03-23  JPP  - Finished tinkering with support for Mono. Compile with conditional compilation symbol 'MONO'
 *                    to enable. On Windows, current problems with Mono:
 *                    - grid lines on virtual lists crashes
 *                    - when grouped, items sometimes are not drawn when any item is scrolled out of view
 *                    - i can't seem to get owner drawing to work
 *                    - when editing cell values, the editing controls always appear behind the listview,
 *                      where they function fine -- the user just can't see them :-)
 * 2008-03-16  JPP  - Added some methods suggested by Chris Marlowe (thanks for the suggestions Chris)
 *                    - ClearObjects()
 *                    - GetCheckedObject(), GetCheckedObjects()
 *                    - GetItemAt() variation that gets both the item and the column under a point
 * 2008-02-28  JPP  - Fixed bug with subitem colors when using OwnerDrawn lists and a RowFormatter.
 * v1.9.1
 * 2008-01-29  JPP  - Fixed bug that caused owner-drawn virtual lists to use 100% CPU
 *                  - Added FlagRenderer to help draw bitwise-OR'ed flag values
 * 2008-01-23  JPP  - Fixed bug (introduced in v1.9) that made alternate row colour with groups not quite right
 *                  - Ensure that DesignerSerializationVisibility.Hidden is set on all non-browsable properties
 *                  - Make sure that sort indicators are shown after changing which columns are visible
 * 2008-01-21  JPP  - Added FastObjectListView
 * v1.9
 * 2008-01-18  JPP  - Added IncrementalUpdate()
 * 2008-01-16  JPP  - Right clicking on column header will allow the user to choose which columns are visible.
 *                    Set SelectColumnsOnRightClick to false to prevent this behaviour.
 *                  - Added ImagesRenderer to draw more than one images in a column
 *                  - Changed the positioning of the empty list m to use all the client area. Thanks to Matze.
 * 2007-12-13  JPP  - Added CopySelectionToClipboard(). Ctrl-C invokes this method. Supports text
 *                    and HTML formats.
 * 2007-12-12  JPP  - Added support for checkboxes via CheckStateGetter and CheckStatePutter properties.
 *                  - Made ObjectListView and OLVColumn into partial classes so that others can extend them.
 * 2007-12-09  JPP  - Added ability to have hidden columns, i.e. columns that the ObjectListView knows
 *                    about but that are not visible to the user. Controlled by OLVColumn.IsVisible.
 *                    Added ColumnSelectionForm to the project to show how it could be used in an application.
 *
 * v1.8
 * 2007-11-26  JPP  - Cell editing fully functional
 * 2007-11-21  JPP  - Added SelectionChanged event. This event is triggered once when the
 *                    selection changes, no matter how many items are selected or deselected (in
 *                    contrast to SelectedIndexChanged which is called once for every row that
 *                    is selected or deselected). Thanks to lupokehl42 (Daniel) for his suggestions and
 *                    improvements on this idea.
 * 2007-11-19  JPP  - First take at cell editing
 * 2007-11-17  JPP  - Changed so that items within a group are not sorted if lastSortOrder == None
 *                  - Only call MakeSortIndicatorImages() if we haven't already made the sort indicators
 *                    (Corrected misspelling in the name of the method too)
 * 2007-11-06  JPP  - Added ability to have secondary sort criteria when sorting
 *                    (SecondarySortColumn and SecondarySortOrder properties)
 *                  - Added SortGroupItemsByPrimaryColumn to allow group items to be sorted by the
 *                    primary column. Previous default was to sort by the grouping column.
 * v1.7
 * No big changes to this version but made to work with ListViewPrinter and released with it.
 *
 * 2007-11-05  JPP  - Changed BaseRenderer to use DrawString() rather than TextRenderer, since TextRenderer
 *                    does not work when printing.
 * v1.6
 * 2007-11-03  JPP  - Fixed some bugs in the rebuilding of DataListView.
 * 2007-10-31  JPP  - Changed to use builtin sort indicators on XP and later. This also avoids alignment
 *                    problems on Vista. (thanks to gravybod for the suggestion and example implementation)
 * 2007-10-21  JPP  - Added MinimumWidth and MaximumWidth properties to OLVColumn.
 *                  - Added ability for BuildList() to preserve selection. Calling BuildList() directly
 *                    tries to preserve selection; calling SetObjects() does not.
 *                  - Added SelectAll() and DeselectAll() methods. Useful for working with large lists.
 * 2007-10-08  JPP  - Added GetNextItem() and GetPreviousItem(), which walk sequentially through the
 *                    listview items, even when the view is grouped.
 *                  - Added SelectedItem property
 * 2007-09-28  JPP  - Optimized aspect-to-string conversion. BuildList() 15% faster.
 *                  - Added empty implementation of RefreshObjects() to VirtualObjectListView since
 *                    RefreshObjects() cannot work on virtual lists.
 * 2007-09-13  JPP  - Corrected bug with custom sorter in VirtualObjectListView (thanks for mpgjunky)
 * 2007-09-07  JPP  - Corrected image scaling bug in DrawAlignedImage() (thanks to krita970)
 * 2007-08-29  JPP  - Allow item count labels on groups to be set per column (thanks to cmarlow for idea)
 * 2007-08-14  JPP  - Major rework of DataListView based on Ian Griffiths's great work
 * 2007-08-11  JPP  - When empty, the control can now draw a "List Empty" m
 *                  - Added GetColumn() and GetItem() methods
 * v1.5
 * 2007-08-03  JPP  - Support animated GIFs in ImageRenderer
 *                  - Allow height of rows to be specified - EXPERIMENTAL!
 * 2007-07-26  JPP  - Optimised redrawing of owner-drawn lists by remembering the update rect
 *                  - Allow sort indicators to be turned off
 * 2007-06-30  JPP  - Added RowFormatter delegate
 *                  - Allow a different label when there is only one item in a group (thanks to cmarlow)
 * v1.4
 * 2007-04-12  JPP  - Allow owner drawn on steriods!
 *                  - Column headers now display sort indicators
 *                  - ImageGetter delegates can now return ints, strings or Images
 *                    (Images are only visible if the list is owner drawn)
 *                  - Added OLVColumn.MakeGroupies to help with group partitioning
 *                  - All normal listview views are now supported
 *                  - Allow dotted aspect names, e.g. Owner.Workgroup.Name (thanks to OlafD)
 *                  - Added SelectedObject and SelectedObjects properties
 * v1.3
 * 2007-03-01  JPP  - Added DataListView
 *                  - Added VirtualObjectListView
 * 					- Added Freeze/Unfreeze capabilities
 *                  - Allowed sort handler to be installed
 *                  - Simplified sort comparisons: handles 95% of cases with only 6 lines of code!
 *                  - Fixed bug with alternative line colors on unsorted lists (thanks to cmarlow)
 * 2007-01-13  JPP  - Fixed bug with lastSortOrder (thanks to Kwan Fu Sit)
 *                  - Non-OLVColumns are no longer allowed
 * 2007-01-04  JPP  - Clear sorter before rebuilding list. 10x faster! (thanks to aaberg)
 *                  - Include GetField in GetAspectByName() so field values can be Invoked too.
 * 					- Fixed subtle bug in RefreshItem() that erased background colors.
 * 2006-11-01  JPP  - Added alternate line colouring
 * 2006-10-20  JPP  - Refactored all sorting comparisons and made it extendable. See ComparerManager.
 *                  - Improved IDE integration
 *                  - Made control DoubleBuffered
 *                  - Added object selection methods
 * 2006-10-13  JPP  Implemented grouping and column sorting
 * 2006-10-09  JPP  Initial version
 *
 * TO DO:
 * - Support undocumented group features: subseted groups, group footer items
 *
 * Copyright (C) 2006-2018 Phillip Piper
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 * If you wish to use this code in a closed source application, please contact phillip.piper@gmail.com.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using MethodInvoker = System.Windows.Forms.MethodInvoker;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Threading;

namespace BrightIdeasSoftware
{
    /// <summary>
    /// An ObjectListView is a much easier to use, and much more powerful, version of the ListView.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An ObjectListView automatically populates a ListView control with information taken 
    /// from a given collection of objects. It can do this because each column is configured
    /// to know which bit of the model object (the "aspect") it should be displaying. Columns similarly
    /// understand how to sort the list based on their aspect, and how to construct groups
    /// using their aspect.
    /// </para>
    /// <para>
    /// Aspects are extracted by giving the name of a method to be called or a
    /// property to be fetched. These names can be simple names or they can be dotted
    /// to chain property access e.g. "Owner.Address.Postcode".
    /// Aspects can also be extracted by installing a delegate.
    /// </para>
    /// <para>
    /// An ObjectListView can show a "this list is empty" message when there is nothing to show in the list, 
    /// so that the user knows the control is supposed to be empty.
    /// </para>
    /// <para>
    /// Right clicking on a column header should present a menu which can contain:
    /// commands (sort, group, ungroup); filtering; and column selection. Whether these
    /// parts of the menu appear is controlled by ShowCommandMenuOnRightClick, 
    /// ShowFilterMenuOnRightClick and SelectColumnsOnRightClick respectively.
    /// </para>
    /// <para>
    /// The groups created by an ObjectListView can be configured to include other formatting
    /// information, including a group icon, subtitle and task button. Using some undocumented
    /// interfaces, these groups can even on virtual lists.
    /// </para>
    /// <para>
    /// ObjectListView supports dragging rows to other places, including other application. 
    /// Special support is provide for drops from other ObjectListViews in the same application. 
    /// In many cases, an ObjectListView becomes a full drag source by setting <see cref="IsSimpleDragSource"/> to 
    /// true. Similarly, to accept drops, it is usually enough to set <see cref="IsSimpleDropSink"/> to true, 
    /// and then handle the <see cref="CanDrop"/>  and <see cref="Dropped"/>  events (or the <see cref="ModelCanDrop"/>  and 
    /// <see cref="ModelDropped"/> events, if you only want to handle drops from other ObjectListViews in your application).
    /// </para>
    /// <para>
    /// For these classes to build correctly, the project must have references to these assemblies:
    /// </para>
    /// <list type="bullet">
    /// <item><description>System</description></item>
    /// <item><description>System.Data</description></item>
    /// <item><description>System.Design</description></item>
    /// <item><description>System.Drawing</description></item>
    /// <item><description>System.Windows.Forms (obviously)</description></item>
    /// </list>
    /// </remarks>
#if NETFRAMEWORK
    [Designer("BrightIdeasSoftware.Design.ObjectListViewDesigner, ObjectListView.Design.Framework")]
#else
    [Designer("ObjectListViewDesigner")]
#endif
    public abstract partial class ObjectListViewBase : ListView, ISupportInitialize {
        private readonly ConditionalWeakTable<OLVListItem, RowThemeColors> rowThemeColors = new ConditionalWeakTable<OLVListItem, RowThemeColors>();

        private sealed class RowThemeColors {
            public Color BackColor;
            public Color ForeColor;
        }

        internal int ModelRowsVersion { get; private set; }
        internal virtual IVirtualGroups ModelRowGroupingStrategy => null;

        internal void InvalidateModelRows() {
            unchecked { this.ModelRowsVersion++; }
        }
        
        #region Life and death
        
        /// <summary>
        /// Create an ObjectListView
        /// </summary>
        protected ObjectListViewBase() {
            this.ColumnClick += new ColumnClickEventHandler(this.HandleColumnClick);
            this.Layout += new LayoutEventHandler(this.HandleLayout);
            this.ColumnWidthChanging += new ColumnWidthChangingEventHandler(this.HandleColumnWidthChanging);
            this.ColumnWidthChanged += new ColumnWidthChangedEventHandler(this.HandleColumnWidthChanged);

            base.View = View.Details;

            // Turn on owner draw so that we are responsible for our own fates (and isolated from bugs in the underlying ListView)
            this.OwnerDraw = true;

// ReSharper disable DoNotCallOverridableMethodsInConstructor
            this.DoubleBuffered = true; // kill nasty flickers. hiss... me hates 'em
            this.ShowSortIndicators = true;

            // Setup the overlays that will be controlled by the IDE settings
            this.InitializeStandardOverlays();
            this.InitializeEmptyListMsgOverlay();
// ReSharper restore DoNotCallOverridableMethodsInConstructor
        }

        /// <summary>
        /// Dispose of any resources this instance has been using
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing) {
            if (disposing)
            {
                this.DropSink = null;
                // ListView.Dispose clears StateImageList before WM_DESTROY can protect its handle.
                if (this.IsHandleCreated && this.StateImageList != null)
                {
                    NativeMethods.SetStateImageList(this, null);
                }
            }

            base.Dispose(disposing);

            if (!disposing)
            {
                return;
            }

            foreach (GlassPanelForm glassPanel in this.glassPanels) {
                glassPanel.Unbind();
                glassPanel.Dispose();
            }
            this.glassPanels.Clear();
            this.standardTextOverlay?.Dispose();
            this.standardEmptyListMsgOverlay?.Dispose();
            this.scrollBarGlass?.Dispose();
            if (this.subItemCheckBoxImageList != null)
            {
                this.subItemCheckBoxImageList.Dispose();
            }
            this.primaryCheckBoxImageList?.Dispose();

            if (this.resizedImageList != null)
            {
                this.resizedImageList.Dispose();
            }

            this.UnsubscribeNotifications(null);
            this.DisposeStyleFonts();
        }

        #endregion

        // TODO
        //public CheckBoxSettings CheckBoxSettings {
        //    get { return checkBoxSettings; }
        //    private set { checkBoxSettings = value; }
        //}

        #region Static properties

        /// <summary>
        /// Gets whether or not the left mouse button is down at this very instant
        /// </summary>
        public static bool IsLeftMouseDown {
            get { return (Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left; }
        }

        /// <summary>
        /// Gets whether the program running on Vista or later?
        /// </summary>
        public static bool IsVistaOrLater {
            get {
                if (!ObjectListViewBase.sIsVistaOrLater.HasValue)
                {
                    ObjectListViewBase.sIsVistaOrLater = Environment.OSVersion.Version.Major >= 6;
                }

                return ObjectListViewBase.sIsVistaOrLater.Value;
            }
        }
        private static bool? sIsVistaOrLater;

        /// <summary>
        /// Gets whether the program running on Win7 or later?
        /// </summary>
        public static bool IsWin7OrLater {
            get {
                if (!ObjectListViewBase.sIsWin7OrLater.HasValue) {
                    // For some reason, Win7 is v6.1, not v7.0
                    Version version = Environment.OSVersion.Version;
                    ObjectListViewBase.sIsWin7OrLater = version.Major > 6 || (version.Major == 6 && version.Minor > 0);
                }
                return ObjectListViewBase.sIsWin7OrLater.Value;
            }
        }
        private static bool? sIsWin7OrLater;

        /// <summary>
        /// Gets or sets how what smoothing mode will be applied to graphic operations.
        /// </summary>
        public static System.Drawing.Drawing2D.SmoothingMode SmoothingMode {
            get { return ObjectListViewBase.sSmoothingMode; }
            set { ObjectListViewBase.sSmoothingMode = value; }
        }
        private static System.Drawing.Drawing2D.SmoothingMode sSmoothingMode =
            System.Drawing.Drawing2D.SmoothingMode.HighQuality;

        /// <summary>
        /// Gets or sets how should text be rendered.
        /// </summary>
        public static System.Drawing.Text.TextRenderingHint TextRenderingHint {
            get { return ObjectListViewBase.sTextRendereringHint; }
            set { ObjectListViewBase.sTextRendereringHint = value; }
        }
        private static System.Drawing.Text.TextRenderingHint sTextRendereringHint =
            System.Drawing.Text.TextRenderingHint.SystemDefault;

        /// <summary>
        /// Gets or sets the string that will be used to title groups when the group key is null.
        /// Exposed so it can be localized.
        /// </summary>
        public static string GroupTitleDefault {
            get { return ObjectListViewBase.sGroupTitleDefault; }
            set { ObjectListViewBase.sGroupTitleDefault = value ?? "{null}"; }
        }
        private static string sGroupTitleDefault = "{null}";

        /// <summary>
        /// Convert the given enumerable into an ArrayList as efficiently as possible
        /// </summary>
        /// <param name="collection">The source collection</param>
        /// <param name="alwaysCreate">If true, this method will always create a new
        /// collection.</param>
        /// <returns>An ArrayList with the same contents as the given collection.</returns>
        /// <remarks>
        /// <para>When we move to .NET 3.5, we can use LINQ and not need this method.</para>
        /// </remarks>
        public static ArrayList EnumerableToArray(IEnumerable collection, bool alwaysCreate) {
            if (collection == null)
            {
                return new ArrayList();
            }

            if (!alwaysCreate) {
                ArrayList array = collection as ArrayList;
                if (array != null)
                {
                    return array;
                }

                IList iList = collection as IList;
                if (iList != null)
                {
                    return ArrayList.Adapter(iList);
                }
            }

            ICollection iCollection = collection as ICollection;
            if (iCollection != null)
            {
                return new ArrayList(iCollection);
            }

            ArrayList newObjects = new ArrayList();
            foreach (object x in collection)
            {
                newObjects.Add(x);
            }

            return newObjects;
        }


        /// <summary>
        /// Return the count of items in the given enumerable
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        /// <remarks>When we move to .NET 3.5, we can use LINQ and not need this method.</remarks>
        public static int EnumerableCount(IEnumerable collection) {
            if (collection == null)
            {
                return 0;
            }

            ICollection iCollection = collection as ICollection;
            if (iCollection != null)
            {
                return iCollection.Count;
            }

            int i = 0;
// ReSharper disable once UnusedVariable
            foreach (object x in collection)
            {
                i++;
            }

            return i;
        }

        /// <summary>
        /// Return whether or not the given enumerable is empty. A string is regarded as 
        /// an empty collection.
        /// </summary>
        /// <param name="collection"></param>
        /// <returns>True if the given collection is null or empty</returns>
        /// <remarks>
        /// <para>When we move to .NET 3.5, we can use LINQ and not need this method.</para>
        /// </remarks>
        public static bool IsEnumerableEmpty(IEnumerable collection) {
            return collection == null || (collection is string) || !collection.GetEnumerator().MoveNext();
        }

        /// <summary>
        /// Gets or sets whether all ObjectListViews will silently ignore missing aspect errors.
        /// </summary>
        /// <remarks>
        /// <para>
        /// By default, if an ObjectListView is asked to display an aspect
        /// (i.e. a field/property/method)
        /// that does not exist from a model, it displays an error message in that cell, since that 
        /// condition is normally a programming error. There are some use cases where
        /// this is not an error -- in those cases, set this to true and ObjectListView will
        /// simply display an empty cell.
        /// </para>
        /// <para>Be warned: if you set this to true, it can be very difficult to track down
        /// typing mistakes or name changes in AspectNames.</para>
        /// </remarks>
        public static bool IgnoreMissingAspects {
            get { return Munger.IgnoreMissingAspects; }
            set { Munger.IgnoreMissingAspects = value; }
        }

        /// <summary>
        /// Gets or sets whether the control will draw a rectangle in each cell showing the cell padding.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This can help with debugging display problems from cell padding.
        /// </para>
        /// <para>As with all cell padding, this setting only takes effect when the control is owner drawn.</para>
        /// </remarks>
        public static bool ShowCellPaddingBounds {
            get { return sShowCellPaddingBounds;  }
            set { sShowCellPaddingBounds = value;  }
        }
        private static bool sShowCellPaddingBounds;

        /// <summary>
        /// Gets the style that will be used by default to format disabled rows
        /// </summary>
        public static SimpleItemStyle DefaultDisabledItemStyle {
            get {
                if (sDefaultDisabledItemStyle == null) {
                    sDefaultDisabledItemStyle = new SimpleItemStyle();
                    sDefaultDisabledItemStyle.ForeColor = Color.DarkGray;
                }
                return sDefaultDisabledItemStyle;
            }
        }
        private static SimpleItemStyle sDefaultDisabledItemStyle;

        /// <summary>
        /// Gets the style that will be used by default to format hot rows
        /// </summary>
        public static HotItemStyle DefaultHotItemStyle {
            get { return ObjectListViewTheme.DefaultHotItemStyle; }
        }

        #endregion

        #region Public properties

        /// <summary>Gets or sets the row background, adapting the default system color to the selected palette.</summary>
        public override Color BackColor {
            get => base.BackColor == SystemColors.Window ? ObjectListViewTheme.Current.WindowColor : base.BackColor;
            set {
                base.BackColor = value;
                if (this.IsHandleCreated)
                {
                    NativeMethods.SetListViewColors(this);
                }
            }
        }

        /// <summary>Gets or sets the row text color, adapting the default system color to the selected palette.</summary>
        public override Color ForeColor {
            get => base.ForeColor == SystemColors.WindowText ? ObjectListViewTheme.Current.TextColor : base.ForeColor;
            set {
                base.ForeColor = value;
                if (this.IsHandleCreated)
                {
                    NativeMethods.SetListViewColors(this);
                }
            }
        }

        /// <summary>
        /// Gets or sets an model filter that is combined with any column filtering that the end-user specifies.
        /// </summary>
        /// <remarks>This is different from the ModelFilter property, since setting that will replace
        /// any column filtering, whereas setting this will combine this filter with the column filtering</remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual IModelFilter AdditionalFilter
        {
            get { return this.additionalFilter; }
            set
            {
                if (this.additionalFilter == value)
                {
                    return;
                }

                this.additionalFilter = value;
                this.UpdateColumnFiltering();
            }
        }
        private IModelFilter additionalFilter;

        /// <summary>
        /// Get or set all the columns that this control knows about.
        /// Only those columns where IsVisible is true will be seen by the user.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If you want to add new columns programmatically, add them to
        /// AllColumns and then call RebuildColumns(). Normally, you do not have to
        /// deal with this property directly. Just use the IDE.
        /// </para>
        /// <para>If you do add or remove columns from the AllColumns collection,
        /// you have to call RebuildColumns() to make those changes take effect.</para>
        /// </remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public virtual List<OLVColumn> AllColumns {
            get { return this.allColumns; }
            set { this.allColumns = value ?? new List<OLVColumn>(); }
        }
        private List<OLVColumn> allColumns = new List<OLVColumn>();

        /// <summary>
        /// Gets or sets whether or not ObjectListView will allow cell editors to response to mouse wheel events. Default is true.
        /// If this is true, cell editors that respond to mouse wheel events (e.g. numeric edit, DateTimeEditor, combo boxes) will operate
        /// as expected.
        /// If this is false, a mouse wheel event is interpreted as a request to scroll the control vertically. This will automatically
        /// finish any cell edit operation that was in flight. This was the default behaviour prior to v2.9.
        /// </summary>
        [Category("ObjectListView"),
        Description("Should ObjectListView allow cell editors to response to mouse wheel events (default: true)"),
        DefaultValue(true)]
        public virtual bool AllowCellEditorsToProcessMouseWheel
        {
            get { return allowCellEditorsToProcessMouseWheel; }
            set { allowCellEditorsToProcessMouseWheel = value; }
        }
        private bool allowCellEditorsToProcessMouseWheel = true;

        /// <summary>
        /// Gets or sets the background color of every second row 
        /// </summary>
        [Category("ObjectListView"),
         Description("If using alternate colors, what color should the background of alternate rows be?"),
         DefaultValue(typeof(Color), "")]
        public Color AlternateRowBackColor {
            get { return alternateRowBackColor; }
            set { alternateRowBackColor = value; }
        }
        private Color alternateRowBackColor = Color.Empty;

        /// <summary>
        /// Gets the alternate row background color that has been set, or the default color
        /// </summary>
        [Browsable(false)]
        public virtual Color AlternateRowBackColorOrDefault {
            get {
                return this.alternateRowBackColor.IsEmpty ? ObjectListViewTheme.Current.ControlColor : this.alternateRowBackColor;
            }
        }

        /// <summary>
        /// Give access to the image list that is actually being used by the control
        /// </summary>
        /// <remarks>
        /// Normally, it is preferable to use SmallImageList. Only use this property
        /// if you know exactly what you are doing.
        /// </remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual ImageList BaseSmallImageList {
            get { return base.SmallImageList; }
            set { base.SmallImageList = value; }
        }

        /// <summary>
        /// How does the user indicate that they want to edit a cell?
        /// None means that the listview cannot be edited.
        /// </summary>
        /// <remarks>Columns can also be marked as editable.</remarks>
        [Category("ObjectListView"),
         Description("How does the user indicate that they want to edit a cell?"),
         DefaultValue(CellEditActivateMode.None)]
        public virtual CellEditActivateMode CellEditActivation {
            get { return cellEditActivation; }
            set {
                cellEditActivation = value;
                if (this.Created)
                {
                    this.Invalidate();
                }
            }
        }
        private CellEditActivateMode cellEditActivation = CellEditActivateMode.None;

        /// <summary>
        /// When a cell is edited, should the whole cell be used (minus any space used by checkbox or image)?
        /// Defaults to true.
        /// </summary>
        /// <remarks>
        /// <para>This is always treated as true when the control is NOT owner drawn.</para>
        /// <para>
        /// When this is false and the control is owner drawn, 
        /// ObjectListView will try to calculate the width of the cell's
        /// actual contents, and then size the editing control to be just the right width. If this is true,
        /// the whole width of the cell will be used, regardless of the cell's contents.
        /// </para>
        /// <para>Each column can have a different value for property. This value from the control is only
        /// used when a column is not specified one way or another.</para>
        /// <para>Regardless of this setting, developers can specify the exact size of the editing control
        /// by listening for the CellEditStarting event.</para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("When a cell is edited, should the whole cell be used?"),
         DefaultValue(true)]
        public virtual bool CellEditUseWholeCell {
            get { return cellEditUseWholeCell; }
            set { cellEditUseWholeCell = value; }
        }
        private bool cellEditUseWholeCell = true;

        /// <summary>
        /// Gets or sets the engine that will handle key presses during a cell edit operation.
        /// Settings this to null will reset it to default value.
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CellEditKeyEngine CellEditKeyEngine {
            get { return this.cellEditKeyEngine ?? (this.cellEditKeyEngine = new CellEditKeyEngine()); }
            set { this.cellEditKeyEngine = value; }
        }
        private CellEditKeyEngine cellEditKeyEngine;

        /// <summary>
        /// Gets the control that is currently being used for editing a cell.
        /// </summary>
        /// <remarks>This will obviously be null if no cell is being edited.</remarks>
        [Browsable(false)]
        public Control CellEditor {
            get {
                return this.cellEditor;
            }
        }

        /// <summary>
        /// Gets or sets the behaviour of the Tab key when editing a cell on the left or right
        /// edge of the control. If this is false (the default), pressing Tab will wrap to the other side
        /// of the same row. If this is true, pressing Tab when editing the right most cell will advance 
        /// to the next row 
        /// and Shift-Tab when editing the left-most cell will change to the previous row.
        /// </summary>
        [Category("ObjectListView"),
        Description("Should Tab/Shift-Tab change rows while cell editing?"),
        DefaultValue(false)]
        public virtual bool CellEditTabChangesRows {
            get { return cellEditTabChangesRows; }
            set {
                cellEditTabChangesRows = value;
                if (cellEditTabChangesRows) {
                    this.CellEditKeyEngine.SetKeyBehaviour(Keys.Tab, CellEditCharacterBehaviour.ChangeColumnRight, CellEditAtEdgeBehaviour.ChangeRow);
                    this.CellEditKeyEngine.SetKeyBehaviour(Keys.Tab|Keys.Shift, CellEditCharacterBehaviour.ChangeColumnLeft, CellEditAtEdgeBehaviour.ChangeRow);
                } else {
                    this.CellEditKeyEngine.SetKeyBehaviour(Keys.Tab, CellEditCharacterBehaviour.ChangeColumnRight, CellEditAtEdgeBehaviour.Wrap);
                    this.CellEditKeyEngine.SetKeyBehaviour(Keys.Tab | Keys.Shift, CellEditCharacterBehaviour.ChangeColumnLeft, CellEditAtEdgeBehaviour.Wrap);
                }
            }
        }
        private bool cellEditTabChangesRows;

        /// <summary>
        /// Gets or sets the behaviour of the Enter keys while editing a cell.
        /// If this is false (the default), pressing Enter will simply finish the editing operation.
        /// If this is true, Enter will finish the edit operation and start a new edit operation
        /// on the cell below the current cell, wrapping to the top of the next row when at the bottom cell.
        /// </summary>
        [Category("ObjectListView"),
        Description("Should Enter change rows while cell editing?"),
        DefaultValue(false)]
        public virtual bool CellEditEnterChangesRows {
            get { return cellEditEnterChangesRows; }
            set {
                cellEditEnterChangesRows = value;
                if (cellEditEnterChangesRows) {
                    this.CellEditKeyEngine.SetKeyBehaviour(Keys.Enter, CellEditCharacterBehaviour.ChangeRowDown, CellEditAtEdgeBehaviour.ChangeColumn);
                    this.CellEditKeyEngine.SetKeyBehaviour(Keys.Enter | Keys.Shift, CellEditCharacterBehaviour.ChangeRowUp, CellEditAtEdgeBehaviour.ChangeColumn);
                } else {
                    this.CellEditKeyEngine.SetKeyBehaviour(Keys.Enter, CellEditCharacterBehaviour.EndEdit, CellEditAtEdgeBehaviour.EndEdit);
                    this.CellEditKeyEngine.SetKeyBehaviour(Keys.Enter | Keys.Shift, CellEditCharacterBehaviour.EndEdit, CellEditAtEdgeBehaviour.EndEdit);
                }
            }
        }
        private bool cellEditEnterChangesRows;

        /// <summary>
        /// Gets the tool tip control that shows tips for the cells
        /// </summary>
        [Browsable(false)]
        public ToolTipControl CellToolTip {
            get {
                if (this.cellToolTip == null) {
                    this.CreateCellToolTip();
                }
                return this.cellToolTip;
            }
        }
        private ToolTipControl cellToolTip;

        /// <summary>
        /// Gets or sets how many pixels will be left blank around each cell of this item.
        /// Cell contents are aligned after padding has been taken into account.
        /// </summary>
        /// <remarks>
        /// <para>Each value of the given rectangle will be treated as an inset from
        /// the corresponding side. The width of the rectangle is the padding for the
        /// right cell edge. The height of the rectangle is the padding for the bottom
        /// cell edge.
        /// </para>
        /// <para>
        /// So, this.olv1.CellPadding = new Rectangle(1, 2, 3, 4); will leave one pixel
        /// of space to the left of the cell, 2 pixels at the top, 3 pixels of space
        /// on the right edge, and 4 pixels of space at the bottom of each cell.
        /// </para>
        /// <para>
        /// This setting only takes effect when the control is owner drawn.
        /// </para>
        /// <para>This setting only affects the contents of the cell. The background is
        /// not affected.</para>
        /// <para>If you set this to a foolish value, your control will appear to be empty.</para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("How much padding will be applied to each cell in this control?"),
         DefaultValue(null)]
        public Rectangle? CellPadding {
            get { return this.cellPadding; }
            set { this.cellPadding = value; }
        }
        private Rectangle? cellPadding;

        /// <summary>
        /// Gets or sets how cells will be vertically aligned by default.
        /// </summary>
        /// <remarks>This setting only takes effect when the control is owner drawn. It will only be noticeable
        /// when RowHeight has been set such that there is some vertical space in each row.</remarks>
        [Category("ObjectListView"),
         Description("How will cell values be vertically aligned?"),
         DefaultValue(StringAlignment.Center)]
        public virtual StringAlignment CellVerticalAlignment {
            get { return this.cellVerticalAlignment;  }
            set { this.cellVerticalAlignment = value;  }
        }
        private StringAlignment cellVerticalAlignment = StringAlignment.Center;

        /// <summary>
        /// Should this list show checkboxes?
        /// </summary>
        [DefaultValue(false)]
        public new bool CheckBoxes {
            get { return base.CheckBoxes; }
            set {
                // Due to code in the base ListView class, turning off CheckBoxes on a virtual
                // list always throws an InvalidOperationException. We have to do some major hacking
                // to get around that
                if (this.VirtualMode) {
                    // Leave virtual mode
                    this.StateImageList = null;
                    this.VirtualListSize = 0;
                    this.VirtualMode = false;

                    // Change the CheckBox setting while not in virtual mode
                    base.CheckBoxes = value;

                    // Reinstate virtual mode
                    this.VirtualMode = true;

                    // Re-enact the bits that we lost by switching to virtual mode
#pragma warning disable CA2245 // Reapply group settings after restoring virtual mode.
                    this.ShowGroups = this.ShowGroups;
#pragma warning restore CA2245
                    this.BuildList(true);
                } else {
                    base.CheckBoxes = value;
                    // Initialize the state image list so we can display indeterminate values.
                    this.InitializeStateImageList();
                }
            }
        }

        /// <summary>
        /// Return the model object of the row that is checked or null if no row is checked
        /// or more than one row is checked
        /// </summary>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual Object CheckedModelObject {
            get {
                IList checkedObjects = this.CheckedModelObjects;
                return checkedObjects.Count == 1 ? checkedObjects[0] : null;
            }
            set {
                this.CheckedModelObjects = new ArrayList(new Object[] { value });
            }
        }

        /// <summary>
        /// Get or set the collection of model objects that are checked.
        /// When setting this property, any row whose model object isn't
        /// in the given collection will be unchecked. Setting to null is
        /// equivalent to unchecking all.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property returns a simple collection. Changes made to the returned
        /// collection do NOT affect the list. This is different to the behaviour of
        /// CheckedIndicies collection.
        /// </para>
        /// <para>
        /// .NET's CheckedItems property is not helpful. It is just a short-hand for
        /// iterating through the list looking for items that are checked.
        /// </para>
        /// <para>The getter returns a materialized snapshot. Use
        /// <see cref="SetCheckedObjects"/> to assign an enumerable sequence.</para>
        /// <para>On non-virtual lists the getter only reports rows that are currently
        /// shown. While a filter is active, checked models that are filtered out are not
        /// returned, although they keep their check state when
        /// <see cref="PersistentCheckBoxes"/> is true and reappear checked once the filter
        /// is removed. The setter, <see cref="CheckAll"/> and <see cref="UncheckAll"/>
        /// apply to all models, including filtered-out ones.</para>
        /// <para>
        /// The performance of the get method is O(n), where n is the number of items
        /// in the control. The performance of the set method is
        /// O(n + m) where m is the number of objects being checked. Be careful on long lists.
        /// </para>
        /// </remarks>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual IList CheckedModelObjects {
            get {
                ArrayList list = new ArrayList();
                if (this.CheckBoxes) {
                    for (int i = 0; i < this.GetItemCount(); i++) {
                        OLVListItem olvi = this.GetItem(i);
                        if (olvi.CheckState == CheckState.Checked)
                        {
                            list.Add(olvi.RowObject);
                        }
                    }
                }
                return list;
            }
            set { this.SetCheckedObjects(value); }
        }

        /// <summary>
        /// Check the given model objects and uncheck all other objects.
        /// </summary>
        /// <remarks>The input is consumed before changing check states. Null unchecks all.
        /// Like CheckedObjects, this method must be called on the control thread and
        /// does nothing when checkboxes are disabled.</remarks>
        public virtual void SetCheckedObjects(IEnumerable modelObjects) {
            if (!this.CheckBoxes)
            {
                return;
            }

            ICollection snapshot = modelObjects == null ? null : this.MaterializeModelObjects(modelObjects);
            Hashtable table = new Hashtable(this.GetItemCount());
            if (snapshot != null) {
                foreach (object x in snapshot)
                {
                    table[x] = true;
                }
            }

            this.BeginUpdate();
            try {
                IEnumerable source = this.ModelObjects;
                if (source != null) {
                    foreach (Object x in source)
                    {
                        this.SetObjectCheckedness(x, table.ContainsKey(x) ? CheckState.Checked : CheckState.Unchecked);
                    }
                }
            }
            finally {
                this.EndUpdate();
            }
        }

        /// <summary>
        /// Gets Columns for this list. We hide the original so we can associate
        /// a specialised editor with it.
        /// </summary>
#if NETFRAMEWORK
        [Editor("BrightIdeasSoftware.Design.OLVColumnCollectionEditor, ObjectListView.Design.Framework", typeof(System.Drawing.Design.UITypeEditor))]
#else
        [Editor("OLVColumnCollectionEditor", "System.Drawing.Design.UITypeEditor")]
#endif
        public new ListView.ColumnHeaderCollection Columns {
            get {
                return base.Columns;
            }
        }

        /// <summary>
        /// Get/set the list of columns that should be used when the list switches to tile view.
        /// </summary>
        [Browsable(false),
        Obsolete("Use GetFilteredColumns() and OLVColumn.IsTileViewColumn instead"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<OLVColumn> ColumnsForTileView {
            get { return this.GetFilteredColumns(View.Tile); }
        }

        /// <summary>
        /// Return the visible columns in the order they are displayed to the user
        /// </summary>
        [Browsable(false)]
        public virtual List<OLVColumn> ColumnsInDisplayOrder {
            get {
                OLVColumn[] columnsInDisplayOrder = new OLVColumn[this.Columns.Count];
                foreach (OLVColumn col in this.Columns) {
                    columnsInDisplayOrder[col.DisplayIndex] = col;
                }
                return new List<OLVColumn>(columnsInDisplayOrder);
            }
        }


        /// <summary>
        /// Get the area of the control that shows the list, minus any header control
        /// </summary>
        [Browsable(false)]
        public Rectangle ContentRectangle {
            get {
                Rectangle r = this.ClientRectangle;

                // If the listview has a header control, remove the header from the control area
                if ((this.View == View.Details || this.ShowHeaderInAllViews) && this.HeaderControl != null) {
                    Rectangle hdrBounds = new Rectangle();
                    NativeMethods.GetClientRect(this.HeaderControl.Handle, ref hdrBounds);
                    r.Y = hdrBounds.Height;
                    r.Height = r.Height - hdrBounds.Height;
                }

                return r;
            }
        }

        /// <summary>
        /// Gets or sets if the selected rows should be copied to the clipboard when the user presses Ctrl-C
        /// </summary>
        [Category("ObjectListView"),
        Description("Should the control copy the selection to the clipboard when the user presses Ctrl-C?"),
        DefaultValue(true)]
        public virtual bool CopySelectionOnControlC {
            get { return copySelectionOnControlC; }
            set { copySelectionOnControlC = value; }
        }
        private bool copySelectionOnControlC = true;


        /// <summary>
        /// Gets or sets whether the Control-C copy to clipboard functionality should use
        /// the installed DragSource to create the data object that is placed onto the clipboard.
        /// </summary>
        /// <remarks>This is normally what is desired, unless a custom DragSource is installed 
        /// that does some very specialized drag-drop behaviour.</remarks>
        [Category("ObjectListView"),
        Description("Should the Ctrl-C copy process use the DragSource to create the Clipboard data object?"),
        DefaultValue(true)]
        public bool CopySelectionOnControlCUsesDragSource {
            get { return this.copySelectionOnControlCUsesDragSource; }
            set { this.copySelectionOnControlCUsesDragSource = value; }
        }
        private bool copySelectionOnControlCUsesDragSource = true;

        /// <summary>
        /// Gets the list of decorations that will be drawn the ListView
        /// </summary>
        /// <remarks>
        /// <para>
        /// Do not modify the contents of this list directly. Use the AddDecoration() and RemoveDecoration() methods.
        /// </para>
        /// <para>
        /// A decoration scrolls with the list contents. An overlay is fixed in place.
        /// </para>
        /// </remarks>
        [Browsable(false)]
        protected IList<IDecoration> Decorations {
            get { return this.decorations; }
        }
        private readonly List<IDecoration> decorations = new List<IDecoration>();

        /// <summary>
        /// When owner drawing, this renderer will draw columns that do not have specific renderer
        /// given to them
        /// </summary>
        /// <remarks>If you try to set this to null, it will revert to a HighlightTextRenderer</remarks>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IRenderer DefaultRenderer {
            get { return this.defaultRenderer; }
            set { this.defaultRenderer = value ?? new HighlightTextRenderer(); }
        }
        private IRenderer defaultRenderer = new HighlightTextRenderer();

        /// <summary>
        /// Get the renderer to be used to draw the given cell.
        /// </summary>
        /// <param name="model">The row model for the row</param>
        /// <param name="column">The column to be drawn</param>
        /// <returns>The renderer used for drawing a cell. Must not return null.</returns>
        public IRenderer GetCellRenderer(object model, OLVColumn column) {
            IRenderer renderer = this.CellRendererGetter == null ? null : this.CellRendererGetter(model, column);
            return renderer ?? column.Renderer ?? this.DefaultRenderer;
        }

        /// <summary>
        /// Gets or sets the style that will be applied to disabled items.
        /// </summary>
        /// <remarks>If this is not set explicitly, <see cref="DefaultDisabledItemStyle"/> will be used.</remarks>
        [Category("ObjectListView"),
        Description("The style that will be applied to disabled items"),
        DefaultValue(null)]
        public SimpleItemStyle DisabledItemStyle
        {
            get { return disabledItemStyle; }
            set { disabledItemStyle = value; }
        }
        private SimpleItemStyle disabledItemStyle;

        /// <summary>
        /// Gets or sets the list of model objects that are disabled.
        /// Disabled objects cannot be selected or activated.
        /// </summary>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual IEnumerable DisabledObjects
        {
            get
            {
                return disabledObjects.Keys;
            }
            set
            {
                this.disabledObjects.Clear();
                DisableObjects(value);
            }
        }
        private readonly Hashtable disabledObjects = new Hashtable();

        /// <summary>
        /// Is this given model object disabled?
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool IsDisabled(object model)
        {
            return model != null && this.disabledObjects.ContainsKey(model);
        }

        /// <summary>
        /// Disable the given model object.
        /// Disabled objects cannot be selected or activated.
        /// </summary>
        /// <param name="model">Must not be null</param>
        public void DisableObject(object model) {
            ArrayList list = new ArrayList();
            list.Add(model);
            this.DisableObjects(list);
        }

        /// <summary>
        /// Disable all the given model objects
        /// </summary>
        /// <param name="models"></param>
        public void DisableObjects(IEnumerable models)
        {
            if (models == null)
            {
                return;
            }

            ArrayList list = ObjectListViewBase.EnumerableToArray(models, false);
            foreach (object model in list)
            {
                if (model == null)
                {
                    continue;
                }

                this.disabledObjects[model] = true;
                int modelIndex = this.IndexOf(model);
                if (modelIndex >= 0)
                {
                    NativeMethods.DeselectOneItem(this, modelIndex);
                }
            }
            this.RefreshObjects(list);
        }

        /// <summary>
        /// Enable the given model object, so it can be selected and activated again.
        /// </summary>
        /// <param name="model">Must not be null</param>
        public void EnableObject(object model)
        {
            this.disabledObjects.Remove(model);
            this.RefreshObject(model);
        }

        /// <summary>
        /// Enable all the given model objects
        /// </summary>
        /// <param name="models"></param>
        public void EnableObjects(IEnumerable models)
        {
            if (models == null)
            {
                return;
            }

            ArrayList list = ObjectListViewBase.EnumerableToArray(models, false);
            foreach (object model in list)
            {
                if (model != null)
                {
                    this.disabledObjects.Remove(model);
                }
            }
            this.RefreshObjects(list);
        }

        /// <summary>
        /// Forget all disabled objects. This does not trigger a redraw or rebuild
        /// </summary>
        protected void ClearDisabledObjects()
        {
            this.disabledObjects.Clear();            
        }

        /// <summary>
        /// Gets or sets the object that controls how drags start from this control
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IDragSource DragSource {
            get { return this.dragSource; }
            set { this.dragSource = value; }
        }
        private IDragSource dragSource;

        /// <summary>
        /// Gets or sets the object that controls how drops are accepted and processed
        /// by this ListView.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the given sink is an instance of SimpleDropSink, then events from the drop sink
        /// will be automatically forwarded to the ObjectListView (which means that handlers
        /// for those event can be configured within the IDE).
        /// </para>
        /// <para>If this is set to null, the control will not accept drops.</para>
        /// </remarks>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IDropSink DropSink {
            get { return this.dropSink; }
            set {
                if (this.dropSink == value)
                {
                    return;
                }

                if (this.dropSink != null && this.dropSink.ListView == this)
                {
                    this.dropSink.ListView = null;
                }

                this.ownedDropSink?.Dispose();
                this.ownedDropSink = null;

                // Stop listening for events on the old sink
                SimpleDropSink oldSink = this.dropSink as SimpleDropSink;
                if (oldSink != null) {
                    oldSink.CanDrop -= new EventHandler<OlvDropEventArgs>(this.DropSinkCanDrop);
                    oldSink.Dropped -= new EventHandler<OlvDropEventArgs>(this.DropSinkDropped);
                    oldSink.ModelCanDrop -= new EventHandler<ModelDropEventArgs>(this.DropSinkModelCanDrop);
                    oldSink.ModelDropped -= new EventHandler<ModelDropEventArgs>(this.DropSinkModelDropped);
                }

                this.dropSink = value;
                this.AllowDrop = (value != null);
                if (this.dropSink != null)
                {
                    this.dropSink.ListView = this;
                }

                // Start listening for events on the new sink
                SimpleDropSink newSink = value as SimpleDropSink;
                if (newSink != null) {
                    newSink.CanDrop += new EventHandler<OlvDropEventArgs>(this.DropSinkCanDrop);
                    newSink.Dropped += new EventHandler<OlvDropEventArgs>(this.DropSinkDropped);
                    newSink.ModelCanDrop += new EventHandler<ModelDropEventArgs>(this.DropSinkModelCanDrop);
                    newSink.ModelDropped += new EventHandler<ModelDropEventArgs>(this.DropSinkModelDropped);
                }
            }
        }
        private IDropSink dropSink;
        private SimpleDropSink ownedDropSink;

        // Forward events from the drop sink to the control itself
        void DropSinkCanDrop(object sender, OlvDropEventArgs e) { this.OnCanDrop(e); }
        void DropSinkDropped(object sender, OlvDropEventArgs e) { this.OnDropped(e); }
        void DropSinkModelCanDrop(object sender, ModelDropEventArgs e) { this.OnModelCanDrop(e); }
        void DropSinkModelDropped(object sender, ModelDropEventArgs e) { this.OnModelDropped(e); }

        /// <summary>
        /// This registry decides what control should be used to edit what cells, based
        /// on the type of the value in the cell.
        /// </summary>
        /// <see cref="EditorRegistry"/>
        /// <remarks>All instances of ObjectListView share the same editor registry.</remarks>
// ReSharper disable FieldCanBeMadeReadOnly.Global
        public static EditorRegistry EditorRegistry { get; set; } = new EditorRegistry();
// ReSharper restore FieldCanBeMadeReadOnly.Global

        /// <summary>
        /// Gets or sets the text that should be shown when there are no items in this list view.
        /// </summary>
        /// <remarks>If the EmptyListMsgOverlay has been changed to something other than a TextOverlay,
        /// this property does nothing</remarks>
        [Category("ObjectListView"),
         Description("When the list has no items, show this message in the control"),
         DefaultValue(null),
         Localizable(true)]
        public virtual String EmptyListMsg {
            get {
                TextOverlay overlay = this.EmptyListMsgOverlay as TextOverlay;
                return overlay == null ? null : overlay.Text;
            }
            set {
                TextOverlay overlay = this.EmptyListMsgOverlay as TextOverlay;
                if (overlay != null) {
                    overlay.Text = value;
                    this.Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets the font in which the List Empty message should be drawn
        /// </summary>
        /// <remarks>If the EmptyListMsgOverlay has been changed to something other than a TextOverlay,
        /// this property does nothing</remarks>
        [Category("ObjectListView"),
        Description("What font should the 'list empty' message be drawn in?"),
        DefaultValue(null)]
        public virtual Font EmptyListMsgFont {
            get {
                TextOverlay overlay = this.EmptyListMsgOverlay as TextOverlay;
                return overlay == null ? null : overlay.Font;
            }
            set {
                TextOverlay overlay = this.EmptyListMsgOverlay as TextOverlay;
                if (overlay != null)
                {
                    overlay.Font = value;
                }
            }
        }

        /// <summary>
        /// Return the font for the 'list empty' message or a reasonable default
        /// </summary>
        [Browsable(false)]
        public virtual Font EmptyListMsgFontOrDefault {
            get {
               return this.EmptyListMsgFont ?? new Font("Tahoma", 14);
            }
        }

        /// <summary>
        /// Gets or sets the overlay responsible for drawing the List Empty msg.
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual IOverlay EmptyListMsgOverlay {
            get { return this.emptyListMsgOverlay; }
            set {
                if (this.emptyListMsgOverlay != value) {
                    this.emptyListMsgOverlay = value;
                    this.Invalidate();
                }
            }
        }
        private IOverlay emptyListMsgOverlay;

        /// <summary>
        /// Gets the collection of objects that survive any filtering that may be in place.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This collection is the result of filtering the current list of objects. 
        /// It is not a snapshot of the filtered list that was last used to build the control. 
        /// </para>
        /// <para>
        /// Normal warnings apply when using this with virtual lists. It will work, but it
        /// may take a while.
        /// </para>
        /// </remarks>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual IEnumerable FilteredObjects {
            get {
                if (this.UseFiltering)
                {
                    return this.FilterObjects(this.ModelObjects, this.ModelFilter, this.ListFilter);
                }

                return this.ModelObjects;
            }
        }

        /// <summary>
        /// Gets or sets the strategy object that will be used to build the Filter menu
        /// </summary>
        /// <remarks>If this is null, no filter menu will be built.</remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FilterMenuBuilder FilterMenuBuildStrategy {
            get { return filterMenuBuilder; }
            set { filterMenuBuilder = value; }
        }
        private FilterMenuBuilder filterMenuBuilder = new FilterMenuBuilder();

        /// <summary>
        /// Gets or sets the row that has keyboard focus
        /// </summary>
        /// <remarks>
        /// <para>
        /// Setting an object to be focused does *not* select it. If you want to select and focus a row,
        /// use <see cref="SelectedModelObject"/>.
        /// </para>
        /// <para>
        /// This property is not generally used and is only useful in specialized situations.
        /// </para>
        /// </remarks>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual Object FocusedObject {
            get { return this.FocusedItem == null ? null : ((OLVListItem)this.FocusedItem).RowObject; }
            set {
                OLVListItem item = this.ModelToItem(value);
                if (item != null)
                {
                    item.Focused = true;
                }
            }
        }

        /// <summary>
        /// Hide the Groups collection so it's not visible in the Properties grid.
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new ListViewGroupCollection Groups {
            get { return base.Groups; }
        }

        /// <summary>
        /// Gets or sets the image list from which group header will take their images
        /// </summary>
        /// <remarks>If this is not set, then group headers will not show any images.</remarks>
        [Category("ObjectListView"),
         Description("The image list from which group header will take their images"),
         DefaultValue(null)]
#if NETFRAMEWORK
        public ImageList GroupImageList {
#else
        public new ImageList GroupImageList {
#endif
            get { return this.groupImageList; }
            set {
                this.groupImageList = value;
                if (this.Created) {
                    NativeMethods.SetGroupImageList(this, value);
                }
            }
        }
        private ImageList groupImageList;

        /// <summary>
        /// Gets how the group label should be formatted when a group is empty or
        /// contains more than one item
        /// </summary>
        /// <remarks>
        /// The given format string must have two placeholders:
        /// <list type="bullet">
        /// <item><description>{0} - the original group title</description></item>
        /// <item><description>{1} - the number of items in the group</description></item>
        /// </list>
        /// </remarks>
        /// <example>"{0} [{1} items]"</example>
        [Category("ObjectListView"),
         Description("The format to use when suffixing item counts to group titles"),
         DefaultValue(null),
         Localizable(true)]
        public virtual string GroupWithItemCountFormat {
            get { return groupWithItemCountFormat; }
            set { groupWithItemCountFormat = value; }
        }
        private string groupWithItemCountFormat;

        /// <summary>
        /// Return this.GroupWithItemCountFormat or a reasonable default
        /// </summary>
        [Browsable(false)]
        public virtual string GroupWithItemCountFormatOrDefault {
            get {
                return String.IsNullOrEmpty(this.GroupWithItemCountFormat) ? "{0} [{1} items]" : this.GroupWithItemCountFormat;
            }
        }

        /// <summary>
        /// Gets how the group label should be formatted when a group contains only a single item
        /// </summary>
        /// <remarks>
        /// The given format string must have two placeholders:
        /// <list type="bullet">
        /// <item><description>{0} - the original group title</description></item>
        /// <item><description>{1} - the number of items in the group (always 1)</description></item>
        /// </list>
        /// </remarks>
        /// <example>"{0} [{1} item]"</example>
        [Category("ObjectListView"),
         Description("The format to use when suffixing item counts to group titles"),
         DefaultValue(null),
         Localizable(true)]
        public virtual string GroupWithItemCountSingularFormat {
            get { return groupWithItemCountSingularFormat; }
            set { groupWithItemCountSingularFormat = value; }
        }
        private string groupWithItemCountSingularFormat;

        /// <summary>
        /// Gets GroupWithItemCountSingularFormat or a reasonable default
        /// </summary>
        [Browsable(false)]
        public virtual string GroupWithItemCountSingularFormatOrDefault {
            get {
                return String.IsNullOrEmpty(this.GroupWithItemCountSingularFormat) ? "{0} [{1} item]" : this.GroupWithItemCountSingularFormat;
            }
        }

        /// <summary>
        /// Gets or sets whether or not the groups in this ObjectListView should be collapsible.
        /// </summary>
        /// <remarks>
        /// This feature only works under Vista and later.
        /// </remarks>
        [Browsable(true),
         Category("ObjectListView"),
         Description("Should the groups in this control be collapsible (Vista and later only)."),
         DefaultValue(true)]
        public bool HasCollapsibleGroups {
            get { return hasCollapsibleGroups; }
            set { hasCollapsibleGroups = value; }
        }
        private bool hasCollapsibleGroups = true;

        /// <summary>
        /// Does this listview have a message that should be drawn when the list is empty?
        /// </summary>
        [Browsable(false)]
        public virtual bool HasEmptyListMsg {
            get { return !String.IsNullOrEmpty(this.EmptyListMsg); }
        }

        /// <summary>
        /// Get whether there are any overlays to be drawn
        /// </summary>
        [Browsable(false)]
        public bool HasOverlays {
            get {
                return (this.Overlays.Count > 2 ||
                    this.imageOverlay.Image != null ||
                    !String.IsNullOrEmpty(this.textOverlay.Text));
            }
        }

        /// <summary>
        /// Gets the header control for the ListView
        /// </summary>
        [Browsable(false)]
        public HeaderControl HeaderControl {
            get { return this.headerControl ?? (this.headerControl = new HeaderControl(this)); }
        }
        private HeaderControl headerControl;

        /// <summary>
        /// Gets or sets the font in which the text of the column headers will be drawn
        /// </summary>
        /// <remarks>Individual columns can override this through their HeaderFormatStyle property.</remarks>
        [DefaultValue(null)]
        [Browsable(false)]
        [Obsolete("Use a HeaderFormatStyle instead", false)]
        public Font HeaderFont {
            get { return this.HeaderFormatStyle == null ? null : this.HeaderFormatStyle.Normal.Font; }
            set {
                if (value == null && this.HeaderFormatStyle == null)
                {
                    return;
                }

                if (this.HeaderFormatStyle == null)
                {
                    this.HeaderFormatStyle = new HeaderFormatStyle();
                }

                this.HeaderFormatStyle.SetFont(value);
            }
        }

        /// <summary>
        /// Gets or sets the style that will be used to draw the column headers of the listview
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is only used when HeaderUsesThemes is false.
        /// </para>
        /// <para>
        /// Individual columns can override this through their HeaderFormatStyle property.
        /// </para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("What style will be used to draw the control's header"),
         DefaultValue(null)]
        public HeaderFormatStyle HeaderFormatStyle {
            get { return this.headerFormatStyle; }
            set { this.headerFormatStyle = value; }
        }
        private HeaderFormatStyle headerFormatStyle;

        /// <summary>
        /// Gets or sets the maximum height of the header. -1 means no maximum.
        /// </summary>
        [Category("ObjectListView"),
         Description("What is the maximum height of the header? -1 means no maximum"),
         DefaultValue(-1)]
        public int HeaderMaximumHeight
        {
            get { return headerMaximumHeight; }
            set { headerMaximumHeight = value; }
        }
        private int headerMaximumHeight = -1;

        /// <summary>
        /// Gets or sets the minimum height of the header. -1 means no minimum.
        /// </summary>
        [Category("ObjectListView"),
         Description("What is the minimum height of the header? -1 means no minimum"),
         DefaultValue(-1)]
        public int HeaderMinimumHeight
        {
            get { return headerMinimumHeight; }
            set { headerMinimumHeight = value; }
        }
        private int headerMinimumHeight = -1;

        /// <summary>
        /// Gets or sets whether the header will be drawn strictly according to the OS's theme. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this is set to true, the header will be rendered completely by the system, without
        /// any of ObjectListViews fancy processing -- no images in header, no filter indicators,
        /// no word wrapping, no header styling, no checkboxes.
        /// </para>
        /// <para>If this is set to false, ObjectListView will render the header as it thinks best.
        /// If no special features are required, then ObjectListView will delegate rendering to the OS.
        /// Otherwise, ObjectListView will draw the header according to the configuration settings.
        /// </para>
        /// <para>
        /// The effect of not being themed will be different from OS to OS. At
        /// very least, the sort indicator will not be standard. 
        /// </para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("Will the column headers be drawn strictly according to OS theme?"),
         DefaultValue(false)]
        public bool HeaderUsesThemes {
            get { return this.headerUsesThemes; }
            set { this.headerUsesThemes = value; }
        }
        private bool headerUsesThemes;

        /// <summary>
        /// Gets or sets the whether the text in the header will be word wrapped.
        /// </summary>
        /// <remarks>
        /// <para>Line breaks will be applied between words. Words that are too long
        /// will still be ellipsed.</para>
        /// <para>
        /// As with all settings that make the header look different, HeaderUsesThemes must be set to false, otherwise
        /// the OS will be responsible for drawing the header, and it does not allow word wrapped text.
        /// </para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("Will the text of the column headers be word wrapped?"),
         DefaultValue(false)]
        public bool HeaderWordWrap {
            get { return this.headerWordWrap; }
            set {
                this.headerWordWrap = value;
                if (this.headerControl != null)
                {
                    this.headerControl.WordWrap = value;
                }
            }
        }
        private bool headerWordWrap;

        /// <summary>
        /// Gets the tool tip that shows tips for the column headers
        /// </summary>
        [Browsable(false)]
        public ToolTipControl HeaderToolTip {
            get {
                return this.HeaderControl.ToolTip;
            }
        }

        /// <summary>
        /// Gets the index of the row that the mouse is currently over
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual int HotRowIndex {
            get { return this.hotRowIndex; }
            protected set { this.hotRowIndex = value; }
        }
        private int hotRowIndex;

        /// <summary>
        /// Gets the index of the subitem that the mouse is currently over
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual int HotColumnIndex {
            get { return this.hotColumnIndex; }
            protected set { this.hotColumnIndex = value; }
        }
        private int hotColumnIndex;

        /// <summary>
        /// Gets the part of the item/subitem that the mouse is currently over
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual HitTestLocation HotCellHitLocation {
            get { return this.hotCellHitLocation; }
            protected set { this.hotCellHitLocation = value; }
        }
        private HitTestLocation hotCellHitLocation;

        /// <summary>
        /// Gets an extended indication of the part of item/subitem/group that the mouse is currently over
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [SuppressMessage("Naming", "CA1711", Justification = "Ex distinguishes extended native hit-test flags from the simplified hit location.")]
        public virtual HitTestLocationEx HotCellHitLocationEx
        {
            get { return this.hotCellHitLocationEx; }
            protected set { this.hotCellHitLocationEx = value; }
        }
        private HitTestLocationEx hotCellHitLocationEx;

        /// <summary>
        /// Gets the group that the mouse is over
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public OLVGroup HotGroup
        {
            get { return hotGroup; }
            internal set { hotGroup = value; }
        }
        private OLVGroup hotGroup;

        /// <summary>
        /// The index of the item that is 'hot', i.e. under the cursor. -1 means no item.
        /// </summary>
        [Browsable(false),
         Obsolete("Use HotRowIndex instead", false)]
        public virtual int HotItemIndex {
            get { return this.HotRowIndex; }
        }

        /// <summary>
        /// What sort of formatting should be applied to the row under the cursor?
        /// </summary>
        /// <remarks>
        /// <para>
        /// This only takes effect when UseHotItem is true.
        /// </para>
        /// <para>If the style has an overlay, it must be set
        /// *before* assigning it to this property. Adding it afterwards will be ignored. </para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("How should the row under the cursor be highlighted"),
         DefaultValue(null)]
        public virtual HotItemStyle HotItemStyle {
            get { return this.hotItemStyle; }
            set {
                if (this.HotItemStyle != null)
                {
                    this.RemoveOverlay(this.HotItemStyle.Overlay);
                }

                this.hotItemStyle = value;
                if (this.HotItemStyle != null)
                {
                    this.AddOverlay(this.HotItemStyle.Overlay);
                }
            }
        }
        private HotItemStyle hotItemStyle;

        /// <summary>
        /// Gets the installed hot item style or a reasonable default.
        /// </summary>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual HotItemStyle HotItemStyleOrDefault {
            get { return this.HotItemStyle ?? ObjectListViewBase.DefaultHotItemStyle; }
        }

        /// <summary>
        /// What sort of formatting should be applied to hyperlinks?
        /// </summary>
        [Category("ObjectListView"),
         Description("How should hyperlinks be drawn"),
         DefaultValue(null)]
        public virtual HyperlinkStyle HyperlinkStyle {
            get { return this.hyperlinkStyle; }
            set { this.hyperlinkStyle = value; }
        }
        private HyperlinkStyle hyperlinkStyle;

        /// <summary>
        /// What color should be used for the background of selected rows?
        /// </summary>
        [Category("ObjectListView"),
         Description("The background of selected rows when the control is owner drawn"),
         DefaultValue(typeof(Color), "")]
        public virtual Color SelectedBackColor {
            get { return this.selectedBackColor; }
            set { this.selectedBackColor = value; }
        }
        private Color selectedBackColor = Color.Empty;

        /// <summary>
        /// Return the color should be used for the background of selected rows or a reasonable default
        /// </summary>
        [Browsable(false)]
        public virtual Color SelectedBackColorOrDefault {
            get {
                return this.SelectedBackColor.IsEmpty ? ObjectListViewTheme.Current.HighlightColor : this.SelectedBackColor;
            }
        }

        /// <summary>
        /// What color should be used for the foreground of selected rows?
        /// </summary>
        [Category("ObjectListView"),
         Description("The foreground color of selected rows (when the control is owner drawn)"),
         DefaultValue(typeof(Color), "")]
        public virtual Color SelectedForeColor {
            get { return this.selectedForeColor; }
            set { this.selectedForeColor = value; }
        }
        private Color selectedForeColor = Color.Empty;

        /// <summary>
        /// Return the color should be used for the foreground of selected rows or a reasonable default
        /// </summary>
        [Browsable(false)]
        public virtual Color SelectedForeColorOrDefault {
            get {
                return this.SelectedForeColor.IsEmpty ? ObjectListViewTheme.Current.HighlightTextColor : this.SelectedForeColor;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Obsolete("Use SelectedBackColor instead")]
        public virtual Color HighlightBackgroundColor { get { return this.SelectedBackColor; } set { this.SelectedBackColor = value; } }

        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Use SelectedBackColorOrDefault instead")]
        public virtual Color HighlightBackgroundColorOrDefault { get { return this.SelectedBackColorOrDefault; } }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Obsolete("Use SelectedForeColor instead")]
        public virtual Color HighlightForegroundColor { get { return this.SelectedForeColor; } set { this.SelectedForeColor = value; } }

        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Use SelectedForeColorOrDefault instead")]
        public virtual Color HighlightForegroundColorOrDefault { get { return this.SelectedForeColorOrDefault; } }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Obsolete("Use UnfocusedSelectedBackColor instead")]
        public virtual Color UnfocusedHighlightBackgroundColor { get { return this.UnfocusedSelectedBackColor; } set { this.UnfocusedSelectedBackColor = value; } }

        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Use UnfocusedSelectedBackColorOrDefault instead")]
        public virtual Color UnfocusedHighlightBackgroundColorOrDefault { get { return this.UnfocusedSelectedBackColorOrDefault; } }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Obsolete("Use UnfocusedSelectedForeColor instead")]
        public virtual Color UnfocusedHighlightForegroundColor { get { return this.UnfocusedSelectedForeColor; } set { this.UnfocusedSelectedForeColor = value; } }

        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Use UnfocusedSelectedForeColorOrDefault instead")]
        public virtual Color UnfocusedHighlightForegroundColorOrDefault { get { return this.UnfocusedSelectedForeColorOrDefault; } }

        /// <summary>
        /// Gets or sets whether or not hidden columns should be included in the text representation
        /// of rows that are copied or dragged to another application. If this is false (the default),
        /// only visible columns will be included.
        /// </summary>
        [Category("ObjectListView"),
        Description("When rows are copied or dragged, will data in hidden columns be included in the text? If this is false, only visible columns will be included."),
        DefaultValue(false)]
        public virtual bool IncludeHiddenColumnsInDataTransfer
        {
            get { return includeHiddenColumnsInDataTransfer; }
            set { includeHiddenColumnsInDataTransfer = value; }
        }
        private bool includeHiddenColumnsInDataTransfer;

        /// <summary>
        /// Gets or sets whether or not hidden columns should be included in the text representation
        /// of rows that are copied or dragged to another application. If this is false (the default),
        /// only visible columns will be included.
        /// </summary>
        [Category("ObjectListView"),
        Description("When rows are copied, will column headers be in the text?."),
        DefaultValue(false)]
        public virtual bool IncludeColumnHeadersInCopy
        {
            get { return includeColumnHeadersInCopy; }
            set { includeColumnHeadersInCopy = value; }
        }
        private bool includeColumnHeadersInCopy;

        /// <summary>
        /// Return true if a cell edit operation is currently happening
        /// </summary>
        [Browsable(false)]
        public virtual bool IsCellEditing {
            get { return this.cellEditor != null; }
        }

        /// <summary>
        /// Return true if the ObjectListView is being used within the development environment.
        /// </summary>
        [Browsable(false)]
        public virtual bool IsDesignMode {
            get { return this.DesignMode; }
        }

        /// <summary>
        /// Gets whether or not the current list is filtering its contents
        /// </summary>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual bool IsFiltering {
            get { return this.UseFiltering && (this.ModelFilter != null || this.ListFilter != null); }
        }

        /// <summary>
        /// When the user types into a list, should the values in the current sort column be searched to find a match?
        /// If this is false, the primary column will always be used regardless of the sort column.
        /// </summary>
        /// <remarks>When this is true, the behavior is like that of ITunes.</remarks>
        [Category("ObjectListView"),
        Description("When the user types into a list, should the values in the current sort column be searched to find a match?"),
        DefaultValue(true)]
        public virtual bool IsSearchOnSortColumn {
            get { return isSearchOnSortColumn; }
            set { isSearchOnSortColumn = value; }
        }
        private bool isSearchOnSortColumn = true;

        /// <summary>
        /// Gets or sets if this control will use a SimpleDropSink to receive drops
        /// </summary>
        /// <remarks>
        /// <para>
        /// Setting this replaces any previous DropSink.
        /// </para>
        /// <para>
        /// After setting this to true, the SimpleDropSink will still need to be configured
        /// to say when it can accept drops and what should happen when something is dropped.
        /// The need to do these things makes this property mostly useless :(
        /// </para>
        /// </remarks>
        [Category("ObjectListView"),
        Description("Should this control will use a SimpleDropSink to receive drops."),
        DefaultValue(false)]
        public virtual bool IsSimpleDropSink {
            get { return this.DropSink != null; }
            set {
                this.DropSink = value ? new SimpleDropSink() : null;
                this.ownedDropSink = this.DropSink as SimpleDropSink;
            }
        }

        /// <summary>
        /// Gets or sets if this control will use a SimpleDragSource to initiate drags
        /// </summary>
        /// <remarks>Setting this replaces any previous DragSource</remarks>
        [Category("ObjectListView"),
        Description("Should this control use a SimpleDragSource to initiate drags out from this control"),
        DefaultValue(false)]
        public virtual bool IsSimpleDragSource {
            get { return this.DragSource != null; }
            set {
                this.DragSource = value ? new SimpleDragSource() : null;
            }
        }

        /// <summary>
        /// Hide the Items collection so it's not visible in the Properties grid.
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new ListViewItemCollection Items {
            get { return base.Items; }
        }

        /// <summary>
        /// This renderer draws the items when in the list is in non-details view.
        /// In details view, the renderers for the individuals columns are responsible.
        /// </summary>
        [Category("ObjectListView"),
        Description("The owner drawn renderer that draws items when the list is in non-Details view."),
        DefaultValue(null)]
        public IRenderer ItemRenderer {
            get { return itemRenderer; }
            set { itemRenderer = value; }
        }
        private IRenderer itemRenderer;

        /// <summary>
        /// Which column did we last sort by
        /// </summary>
        /// <remarks>This is an alias for PrimarySortColumn</remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual OLVColumn LastSortColumn {
            get { return this.PrimarySortColumn; }
            set { this.PrimarySortColumn = value; }
        }

        /// <summary>
        /// Which direction did we last sort
        /// </summary>
        /// <remarks>This is an alias for PrimarySortOrder</remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual SortOrder LastSortOrder {
            get { return this.PrimarySortOrder; }
            set { this.PrimarySortOrder = value; }
        }

        /// <summary>
        /// Gets or  sets the filter that is applied to our whole list of objects.
        /// </summary>
        /// <remarks>
        /// The list is updated immediately to reflect this filter. 
        /// </remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual IListFilter ListFilter {
            get { return listFilter; }
            set {
                listFilter = value;
                if (this.UseFiltering)
                {
                    this.UpdateFiltering();
                }
            }
        }
        private IListFilter listFilter;

        /// <summary>
        /// Gets or  sets the filter that is applied to each model objects in the list
        /// </summary>
        /// <remarks>
        /// <para>You may want to consider using <see cref="AdditionalFilter"/> instead of this property,
        /// since AdditionalFilter combines with column filtering at runtime. Setting this property simply
        /// replaces any column filter the user may have given.</para>
        /// <para>
        /// The list is updated immediately to reflect this filter. 
        /// </para>
        /// </remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual IModelFilter ModelFilter {
            get { return modelFilter; }
            set {
                modelFilter = value;
                this.NotifyNewModelFilter();
                if (this.UseFiltering) {
                    this.UpdateFiltering();

                    // When the filter changes, it's likely/possible that the selection has also changed.
                    // It's expensive to see if the selection has actually changed (for large virtual lists),
                    // so we just fake a selection changed event, just in case. SF #144
                    this.OnSelectedIndexChanged(EventArgs.Empty);
                }
            }
        }
        private IModelFilter modelFilter;

        /// <summary>
        /// Gets the hit test info last time the mouse was moved.
        /// </summary>
        /// <remarks>Useful for hot item processing.</remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual OlvListViewHitTestInfo MouseMoveHitTest {
            get { return mouseMoveHitTest; }
            private set { mouseMoveHitTest = value; }
        }
        private OlvListViewHitTestInfo mouseMoveHitTest;
        
        /// <summary>
        /// Gets or sets the list of groups shown by the listview.
        /// </summary>
        /// <remarks>
        /// This property does not work like the .NET Groups property. It should
        /// be treated as a read-only property.
        /// Changes made to the list are NOT reflected in the ListView itself -- it is pointless to add
        /// or remove groups to/from this list. Such modifications will do nothing.
        /// To do such things, you must listen for
        /// BeforeCreatingGroups or AboutToCreateGroups events, and change the list of
        /// groups in those events.
        /// </remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList<OLVGroup> OLVGroups {
            get { return this.olvGroups; }
            set { this.olvGroups = value; }
        }
        private IList<OLVGroup> olvGroups;

        /// <summary>
        /// Gets or sets the collection of OLVGroups that are collapsed.
        /// </summary>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IEnumerable<OLVGroup> CollapsedGroups {
            get
            {
                if (this.OLVGroups != null)
                {
                    foreach (OLVGroup group in this.OLVGroups)
                    {
                        if (group.Collapsed)
                        {
                            yield return group;
                        }
                    }
                }
            }
            set
            {
                if (this.OLVGroups == null)
                {
                    return;
                }

                NullableDictionary<object, bool> shouldCollapse = new NullableDictionary<object, bool>();
                if (value != null)
                {
                    foreach (OLVGroup group in value)
                    {
                        shouldCollapse[group.Key] = true;
                    }
                }
                foreach (OLVGroup group in this.OLVGroups)
                {
                    group.Collapsed = shouldCollapse.ContainsKey(group.Key);
                }

            }
        }

        /// <summary>
        /// Gets or sets whether the user wants to owner draw the header control
        /// themselves. If this is false (the default), ObjectListView will use
        /// custom drawing to render the header, if needed.
        /// </summary>
        /// <remarks>
        /// If you listen for the DrawColumnHeader event, you need to set this to true,
        /// otherwise your event handler will not be called.
        /// </remarks>
        [Category("ObjectListView"),
         Description("Should the DrawColumnHeader event be triggered"),
         DefaultValue(false)]
        public bool OwnerDrawnHeader {
            get { return ownerDrawnHeader; }
            set { ownerDrawnHeader = value; }
        }
        private bool ownerDrawnHeader;

        /// <summary>
        /// Get/set the collection of objects that this list will show
        /// </summary>
        /// <remarks>
        /// <para>
        /// The contents of the control will be updated immediately after setting this property.
        /// </para>
        /// <para>This method preserves selection, if possible. Use <see cref="SetObjects(IEnumerable, bool)"/> if
        /// you do not want to preserve the selection. Preserving selection is the slowest part of this
        /// code and performance is O(n) where n is the number of selected rows.</para>
        /// <para>This method is not thread safe.</para>
        /// <para>The property DOES work on virtual lists: setting is problem-free, but if you try to get it
        /// and the list has 10 million objects, it may take some time to return.</para>
        /// <para>This collection is unfiltered. Use <see cref="FilteredObjects"/> to access just those objects
        /// that survive any installed filters.</para>
        /// </remarks>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual IEnumerable ModelObjects {
            get { return this.StoredModelObjects; }
            set { this.SetObjects(value, true); }
        }

        /// <summary>
        /// The owned model collection. Virtual controls use their data source instead.
        /// </summary>
        protected abstract IEnumerable StoredModelObjects { get; }

        /// <summary>
        /// Materialize and validate model objects before modifying the control.
        /// </summary>
        protected abstract ICollection MaterializeModelObjects(IEnumerable collection);

        /// <summary>
        /// Adopt the private, validated snapshot as the new owned collection.
        /// </summary>
        protected abstract void SetStoredModelObjects(IEnumerable collection);

        /// <summary>
        /// Insert a private, validated snapshot, optionally retaining nulls.
        /// </summary>
        protected abstract void InsertStoredModelObjects(int index, IEnumerable collection, bool includeNulls);

        /// <summary>
        /// Remove the first matching model from the owned collection.
        /// </summary>
        protected abstract void RemoveStoredModelObject(object modelObject);

        /// <summary>
        /// Replace the first matching model, returning whether it was found.
        /// </summary>
        protected abstract bool ReplaceStoredModelObject(object oldModel, object newModel);

        /// <summary>
        /// Gets the collection of objects that will be considered when creating clusters
        /// (which are used to generate Excel-like column filters)
        /// </summary>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual IEnumerable ObjectsForClustering {
            get { return this.ModelObjects; }
        }

        /// <summary>
        /// Gets or sets the image that will be drawn over the top of the ListView
        /// </summary>
        [Category("ObjectListView"),
         Description("The image that will be drawn over the top of the ListView"),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ImageOverlay OverlayImage {
            get { return this.imageOverlay; }
            set {
                if (this.imageOverlay == value)
                {
                    return;
                }

                this.RemoveOverlay(this.imageOverlay);
                this.imageOverlay = value;
                this.AddOverlay(this.imageOverlay);
            }
        }
        private ImageOverlay imageOverlay;

        /// <summary>
        /// Gets or sets the text that will be drawn over the top of the ListView
        /// </summary>
        [Category("ObjectListView"),
         Description("The text that will be drawn over the top of the ListView"),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public TextOverlay OverlayText {
            get { return this.textOverlay; }
            set {
                if (this.textOverlay == value)
                {
                    return;
                }

                this.RemoveOverlay(this.textOverlay);
                this.textOverlay = value;
                this.AddOverlay(this.textOverlay);
            }
        }
        private TextOverlay textOverlay;
        private TextOverlay standardTextOverlay;
        private TextOverlay standardEmptyListMsgOverlay;

        /// <summary>
        /// Gets or sets the transparency of all the overlays.
        /// 0 is completely transparent, 255 is completely opaque.
        /// </summary>
        /// <remarks>
        /// This is obsolete. Use Transparency on each overlay.
        /// </remarks>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int OverlayTransparency {
            get { return this.overlayTransparency; }
            set { this.overlayTransparency = Math.Min(255, Math.Max(0, value)); }
        }
        private int overlayTransparency = 128;

        /// <summary>
        /// Gets the list of overlays that will be drawn on top of the ListView
        /// </summary>
        /// <remarks>
        /// You can add new overlays and remove overlays that you have added, but
        /// don't mess with the overlays that you didn't create.
        /// </remarks>
        [Browsable(false)]
        protected IList<IOverlay> Overlays {
            get { return this.overlays; }
        }
        private readonly List<IOverlay> overlays = new List<IOverlay>();

        /// <summary>
        /// Gets or sets whether the ObjectListView will be owner drawn. Defaults to true.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When this is true, all of ObjectListView's neat features are available.
        /// </para>
        /// <para>We have to reimplement this property, even though we just call the base
        /// property, in order to change the [DefaultValue] to true.
        /// </para>
        /// </remarks>
        [Category("Appearance"),
         Description("Should the ListView do its own rendering"),
         DefaultValue(true)]
        public new bool OwnerDraw {
            get { return base.OwnerDraw; }
            set { base.OwnerDraw = value; }
        }

        /// <summary>
        /// Gets or sets whether or not primary checkboxes will persistent their values across list rebuild
        /// and filtering operations.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is only useful when you don't explicitly set CheckStateGetter/Putter.
        /// If you use CheckStateGetter/Putter, the checkedness of a row will already be persisted
        /// by those methods. 
        /// </para>
        /// <para>This defaults to true. If this is false, checkboxes will lose their values when the
        /// list if rebuild or filtered.
        ///  If you set it to false on virtual lists,
        /// you have to install CheckStateGetter/Putters.</para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("Will primary checkboxes persistent their values across list rebuilds"),
         DefaultValue(true)]
        public virtual bool PersistentCheckBoxes {
            get { return persistentCheckBoxes; }
            set {
                if (persistentCheckBoxes == value)
                {
                    return;
                }

                persistentCheckBoxes = value;
                this.ClearPersistentCheckState();
            }
        }
        private bool persistentCheckBoxes = true;

        /// <summary>
        /// Gets or sets a dictionary that remembers the check state of model objects
        /// </summary>
        /// <remarks>This is used when PersistentCheckBoxes is true and for virtual lists.</remarks>
        protected Dictionary<Object, CheckState> CheckStateMap {
            get { return checkStateMap ?? (checkStateMap = new Dictionary<object, CheckState>()); }
            set { checkStateMap = value; }
        }
        private Dictionary<Object, CheckState> checkStateMap;

        /// <summary>
        /// Which column did we last sort by
        /// </summary>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual OLVColumn PrimarySortColumn {
            get { return this.sortDescriptions.GetColumn(0); }
            set {
                this.sortDescriptions.SetColumn(0, value);
                if (this.TintSortColumn)
                {
                    this.SelectedColumn = value;
                }
            }
        }

        /// <summary>
        /// Which direction did we last sort
        /// </summary>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual SortOrder PrimarySortOrder {
            get { return this.sortDescriptions.GetOrder(0); }
            set { this.sortDescriptions.SetOrder(0, value); }
        }

        /// <summary>
        /// Gets the active sort columns and directions in priority order.
        /// Shift-clicking a header appends a column or toggles its direction.
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList<OLVSortDescription> SortDescriptions {
            get {
                return this.sortOperation?.Descriptions ?? this.sortDescriptions.GetActiveDescriptions();
            }
        }
        private readonly SortDescriptionCollection sortDescriptions = new SortDescriptionCollection();
        private SortDescriptionCollection requestedSortDescriptions;
        private SortOperation sortOperation;

        private sealed class SortOperation {
            internal SortOperation(IEnumerable<OLVSortDescription> descriptions) {
                this.Configuration = new SortDescriptionCollection(descriptions);
                this.Descriptions = this.Configuration.GetActiveDescriptions();
            }

            internal readonly SortDescriptionCollection Configuration;
            internal readonly IList<OLVSortDescription> Descriptions;
            internal bool Canceled {
                get { return this.state == State.Canceled; }
            }

            internal void Cancel() {
                if (this.state != State.Committed)
                {
                    this.state = State.Canceled;
                }
            }

            internal void Commit() {
                this.state = State.Committed;
            }

            private State state;
            private enum State {
                Pending,
                Canceled,
                Committed
            }
        }

        /// <summary>
        /// Gets the one-based sort priority of a column, or zero if it is not sorted.
        /// </summary>
        public int GetSortPriority(OLVColumn column) {
            IList<OLVSortDescription> descriptions = this.SortDescriptions;
            for (int i = 0; i < descriptions.Count; i++) {
                if (descriptions[i].Column == column)
                {
                    return i + 1;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets or sets if non-editable checkboxes are drawn as disabled. Default is false.
        /// </summary>
        /// <remarks>
        /// <para>This only has effect in owner drawn mode.</para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("Should non-editable checkboxes be drawn as disabled?"),
         DefaultValue(false)]
        public virtual bool RenderNonEditableCheckboxesAsDisabled {
            get { return renderNonEditableCheckboxesAsDisabled; }
            set { renderNonEditableCheckboxesAsDisabled = value; }
        }
        private bool renderNonEditableCheckboxesAsDisabled;

        /// <summary>
        /// Specify the height of each row in the control in pixels.
        /// </summary>
        /// <remarks><para>The row height in a listview is normally determined by the font size and the small image list size.
        /// This setting allows that calculation to be overridden (within reason: you still cannot set the line height to be
        /// less than the line height of the font used in the control). </para>
        /// <para>Setting it to -1 means use the normal calculation method.</para>
        /// <para><bold>This feature is experimental!</bold> Strange things may happen to your program,
        /// your spouse or your pet if you use it.</para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("Specify the height of each row in pixels. -1 indicates default height"),
         DefaultValue(-1)]
        public virtual int RowHeight {
            get { return rowHeight; }
            set {
                if (value < 1)
                {
                    rowHeight = -1;
                }
                else
                {
                    rowHeight = value;
                }

                if (this.DesignMode)
                {
                    return;
                }

                this.SetupBaseImageList();
                if (this.CheckBoxes)
                {
                    this.InitializeStateImageList();
                }
            }
        }
        private int rowHeight = -1;

        /// <summary>
        /// How many pixels high is each row?
        /// </summary>
        [Browsable(false)]
        public virtual int RowHeightEffective {
            get {
                switch (this.View) {
                    case View.List:
                    case View.SmallIcon:
                    case View.Details:
                        return Math.Max(this.SmallImageSize.Height, this.Font.Height);

                    case View.Tile:
                        return this.TileSize.Height;

                    case View.LargeIcon:
                        if (this.LargeImageList == null)
                        {
                            return this.Font.Height;
                        }

                        return Math.Max(this.LargeImageList.ImageSize.Height, this.Font.Height);

                    default:
                        // This should never happen
                        return 0;
                }
            }
        }

        /// <summary>
        /// How many rows appear on each page of this control
        /// </summary>
        [Browsable(false)]
        public virtual int RowsPerPage {
            get {
                return NativeMethods.GetCountPerPage(this);
            }
        }

        /// <summary>
        /// Get/set the column that will be used to resolve comparisons that are equal when sorting.
        /// </summary>
        /// <remarks><para>Shift-click a column header to add a secondary or subsequent sort field.</para>
        /// <para>When this is null, rows that compare equal on the primary sort column are not
        /// ordered by any other column. Unlike a v2.x ObjectListView, the first column is no
        /// longer used as an implicit tie-breaker. Set this property (or use multi-column
        /// sorting) to get a deterministic order for tied rows.</para></remarks>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual OLVColumn SecondarySortColumn {
            get { return this.sortDescriptions.GetColumn(1); }
            set { this.sortDescriptions.SetColumn(1, value); }
        }

        /// <summary>
        /// When the SecondarySortColumn is used, in what order will it compare results?
        /// </summary>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual SortOrder SecondarySortOrder {
            get { return this.sortDescriptions.GetOrder(1); }
            set { this.sortDescriptions.SetOrder(1, value); }
        }

        /// <summary>
        /// Gets or sets if all rows should be selected when the user presses Ctrl-A
        /// </summary>
        [Category("ObjectListView"),
        Description("Should the control select all rows when the user presses Ctrl-A?"),
        DefaultValue(true)]
        public virtual bool SelectAllOnControlA {
            get { return selectAllOnControlA; }
            set { selectAllOnControlA = value; }
        }
        private bool selectAllOnControlA = true;

        /// <summary>
        /// When the user right clicks on the column headers, should a menu be presented which will allow
        /// them to choose which columns will be shown in the view?
        /// </summary>
        /// <remarks>This is just a compatibility wrapper for the SelectColumnsOnRightClickBehaviour
        /// property.</remarks>
        [Category("ObjectListView"),
        Description("When the user right clicks on the column headers, should a menu be presented which will allow them to choose which columns will be shown in the view?"),
        DefaultValue(true)]
        public virtual bool SelectColumnsOnRightClick {
            get { return this.SelectColumnsOnRightClickBehaviour != ColumnSelectBehaviour.None; }
            set {
                if (value) {
                    if (this.SelectColumnsOnRightClickBehaviour == ColumnSelectBehaviour.None)
                    {
                        this.SelectColumnsOnRightClickBehaviour = ColumnSelectBehaviour.InlineMenu;
                    }
                } else {
                    this.SelectColumnsOnRightClickBehaviour = ColumnSelectBehaviour.None;
                }
            }
        }

        /// <summary>
        /// Gets or sets how the user will be able to select columns when the header is right clicked
        /// </summary>
        [Category("ObjectListView"),
        Description("When the user right clicks on the column headers, how will the user be able to select columns?"),
        DefaultValue(ColumnSelectBehaviour.InlineMenu)]
        public virtual ColumnSelectBehaviour SelectColumnsOnRightClickBehaviour {
            get { return selectColumnsOnRightClickBehaviour; }
            set { selectColumnsOnRightClickBehaviour = value; }
        }
        private ColumnSelectBehaviour selectColumnsOnRightClickBehaviour = ColumnSelectBehaviour.InlineMenu;

        /// <summary>
        /// When the column select menu is open, should it stay open after an item is selected?
        /// Staying open allows the user to turn more than one column on or off at a time.
        /// </summary>
        /// <remarks>This only works when SelectColumnsOnRightClickBehaviour is set to InlineMenu.
        /// It has no effect when the behaviour is set to SubMenu.</remarks>
        [Category("ObjectListView"),
        Description("When the column select inline menu is open, should it stay open after an item is selected?"),
        DefaultValue(true)]
        public virtual bool SelectColumnsMenuStaysOpen {
            get { return selectColumnsMenuStaysOpen; }
            set { selectColumnsMenuStaysOpen = value; }
        }
        private bool selectColumnsMenuStaysOpen = true;

        /// <summary>
        /// Gets or sets the column that is drawn with a slight tint.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If TintSortColumn is true, the sort column will automatically
        /// be made the selected column.
        /// </para>
        /// <para>
        /// The colour of the tint is controlled by SelectedColumnTint.
        /// </para>
        /// </remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public OLVColumn SelectedColumn {
            get { return this.selectedColumn; }
            set {
                this.selectedColumn = value;
                if (value == null) {
                    this.RemoveDecoration(this.selectedColumnDecoration);
                } else {
                    if (!this.HasDecoration(this.selectedColumnDecoration))
                    {
                        this.AddDecoration(this.selectedColumnDecoration);
                    }
                }
            }
        }
        private OLVColumn selectedColumn;
        private readonly TintedColumnDecoration selectedColumnDecoration = new TintedColumnDecoration();

        /// <summary>
        /// Gets or sets the decoration that will be drawn on all selected rows
        /// </summary>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual IDecoration SelectedRowDecoration {
            get { return this.selectedRowDecoration; }
            set { this.selectedRowDecoration = value; }
        }
        private IDecoration selectedRowDecoration;

        /// <summary>
        /// What color should be used to tint the selected column?
        /// </summary>
        /// <remarks>
        /// The tint color must be alpha-blendable, so if the given color is solid
        /// (i.e. alpha = 255), it will be changed to have a reasonable alpha value.
        /// </remarks>
        [Category("ObjectListView"),
         Description("The color that will be used to tint the selected column"),
         DefaultValue(typeof(Color), "")]
        public virtual Color SelectedColumnTint {
            get { return selectedColumnTint; }
            set {
                this.selectedColumnTint = value.A == 255 ? Color.FromArgb(15, value) : value;
                this.selectedColumnDecoration.Tint = this.selectedColumnTint;
            }
        }
        private Color selectedColumnTint = Color.Empty;

        /// <summary>
        /// Gets or sets the index of the row that is currently selected. 
        /// When getting the index, if no row is selected,or more than one is selected, return -1.
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual int SelectedIndex {
            get { return this.SelectedIndices.Count == 1 ? this.SelectedIndices[0] : -1; }
            set {
                this.SelectedIndices.Clear();
                if (value >= 0 && value < this.Items.Count)
                {
                    this.SelectedIndices.Add(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the ListViewItem that is currently selected . If no row is selected, or more than one is selected, return null.
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual OLVListItem SelectedItem {
            get {
                return this.SelectedIndices.Count == 1 ? this.GetItem(this.SelectedIndices[0]) : null;
            }
            set {
                this.SelectedIndices.Clear();
                if (value != null)
                {
                    this.SelectedIndices.Add(value.Index);
                }
            }
        }

        /// <summary>
        /// Gets the model object from the currently selected row, if there is only one row selected. 
        /// If no row is selected, or more than one is selected, returns null.
        /// When setting, this will select the row that is displaying the given model object and focus on it. 
        /// All other rows are deselected.
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual Object SelectedModelObject {
            get {
                return this.SelectedIndices.Count == 1 ? this.GetModelAt(this.SelectedIndices[0]) : null;
            }
            set {
                // If the given model is already selected, don't do anything else (prevents an flicker)
                object selectedObject = this.SelectedModelObject;
                if (selectedObject != null && selectedObject.Equals(value))
                {
                    return;
                }

                this.SelectedIndices.Clear();
                this.SelectObject(value, true); 
            }
        }

        /// <summary>
        /// Get the model objects from the currently selected rows. If no row is selected, the returned List will be empty.
        /// When setting this value, select the rows that is displaying the given model objects. All other rows are deselected.
        /// </summary>
        /// <remarks>The getter returns a materialized snapshot. The setter consumes its input
        /// before clearing the current selection. Null clears the selection. Use
        /// <see cref="SetSelectedObjects"/> to assign an enumerable sequence.</remarks>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual IList SelectedModelObjects {
            get {
                ArrayList list = new ArrayList();
                foreach (int index in this.SelectedIndices)
                {
                    list.Add(this.GetModelAt(index));
                }

                return list;
            }
            set {
                this.SetSelectedObjects(value);
            }
        }

        /// <summary>
        /// When the user right clicks on the column headers, should a menu be presented which will allow
        /// them to choose common tasks to perform on the listview?
        /// </summary>
        [Category("ObjectListView"),
        Description("When the user right clicks on the column headers, should a menu be presented which will allow them to perform common tasks on the listview?"),
        DefaultValue(false)]
        public virtual bool ShowCommandMenuOnRightClick {
            get { return showCommandMenuOnRightClick; }
            set { showCommandMenuOnRightClick = value; }
        }
        private bool showCommandMenuOnRightClick;

        /// <summary>
        /// Gets or sets whether this ObjectListView will show Excel like filtering
        /// menus when the header control is right clicked
        /// </summary>
        [Category("ObjectListView"),
         Description("If this is true, right clicking on a column header will show a Filter menu option"),
         DefaultValue(true)]
        public bool ShowFilterMenuOnRightClick {
            get { return showFilterMenuOnRightClick; }
            set { showFilterMenuOnRightClick = value; }
        }
        private bool showFilterMenuOnRightClick = true;

        /// <summary>
        /// Should this list show its items in groups?
        /// </summary>
        [Category("Appearance"),
         Description("Should the list view show items in groups?"),
         DefaultValue(true)]
        public new virtual bool ShowGroups {
            get { return base.ShowGroups; }
            set {
                if (base.ShowGroups != value)
                    this.InvalidateModelRows();
#pragma warning disable CA2245 // Reinstall the native group image list.
                this.GroupImageList = this.GroupImageList;
#pragma warning restore CA2245
                base.ShowGroups = value; 
            }
        }

        /// <summary>
        /// Should the list view show a bitmap in the column header to show the sort direction?
        /// </summary>
        /// <remarks>
        /// The only reason for not wanting to have sort indicators is that, on pre-XP versions of
        /// Windows, having sort indicators required the ListView to have a small image list, and
        /// as soon as you give a ListView a SmallImageList, the text of column 0 is bumped 16
        /// pixels to the right, even if you never used an image.
        /// </remarks>
        [Category("ObjectListView"),
         Description("Should the list view show sort indicators in the column headers?"),
         DefaultValue(true)]
        public virtual bool ShowSortIndicators {
            get { return showSortIndicators; }
            set { showSortIndicators = value; }
        }
        private bool showSortIndicators;

        /// <summary>
        /// Should the list view show images on subitems?
        /// </summary>
        /// <remarks>
        /// <para>Virtual lists have to be owner drawn in order to show images on subitems</para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("Should the list view show images on subitems?"),
         DefaultValue(false)]
        public virtual bool ShowImagesOnSubItems {
            get { return showImagesOnSubItems; }
            set {
                showImagesOnSubItems = value;
                if (this.Created)
                {
                    this.ApplyExtendedStyles();
                }

                if (value && this.VirtualMode)
                {
                    this.OwnerDraw = true;
                }
            }
        }
        private bool showImagesOnSubItems;

        /// <summary>
        /// This property controls whether group labels will be suffixed with a count of items.
        /// </summary>
        /// <remarks>
        /// The format of the suffix is controlled by GroupWithItemCountFormat/GroupWithItemCountSingularFormat properties
        /// </remarks>
        [Category("ObjectListView"),
         Description("Will group titles be suffixed with a count of the items in the group?"),
         DefaultValue(false)]
        public virtual bool ShowItemCountOnGroups {
            get { return showItemCountOnGroups; }
            set { showItemCountOnGroups = value; }
        }
        private bool showItemCountOnGroups;

        /// <summary>
        /// Gets or sets whether the control will show column headers in all
        /// views (true), or only in Details view (false)
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is not working correctly. JPP 2010/04/06.
        /// It works fine if it is set before the control is created.
        /// But if it turned off once the control is created, the control
        /// loses its checkboxes (weird!)
        /// </para>
        /// <para>
        /// To changed this setting after the control is created, things
        /// are complicated. If it is off and we want it on, we have
        /// to change the View and the header will appear. If it is currently
        /// on and we want to turn it off, we have to both change the view
        /// AND recreate the handle. Recreating the handle is a problem 
        /// since it makes our checkbox style disappear. 
        /// </para>
        /// <para>
        /// This property doesn't work on XP.</para>
        /// </remarks>
        [Category("ObjectListView"),
        Description("Will the control will show column headers in all views?"),
        DefaultValue(true)]
        public bool ShowHeaderInAllViews {
            get { return ObjectListViewBase.IsVistaOrLater && showHeaderInAllViews; }
            set {
                if (showHeaderInAllViews == value)
                {
                    return;
                }

                showHeaderInAllViews = value;

                // If the control isn't already created, everything is fine.
                if (!this.Created)
                {
                    return;
                }

                // If the header is being hidden, we have to recreate the control
                // to remove the style (not sure why this is)
                if (showHeaderInAllViews)
                {
                    this.ApplyExtendedStyles();
                }
                else
                {
                    this.RecreateHandle();
                }

                // Still more complications. The change doesn't become visible until the View is changed
                if (this.View != View.Details) {
                    View temp = this.View;
                    this.View = View.Details;
                    this.View = temp;
                }
            }
        }
        private bool showHeaderInAllViews = true;

        /// <summary>
        /// Override the SmallImageList property so we can correctly shadow its operations.
        /// </summary>
        /// <remarks><para>If you use the RowHeight property to specify the row height, the SmallImageList
        /// must be fully initialised before setting/changing the RowHeight. If you add new images to the image
        /// list after setting the RowHeight, you must assign the imagelist to the control again. Something as simple
        /// as this will work:
        /// <code>listView1.SmallImageList = listView1.SmallImageList;</code></para>
        /// </remarks>
        [DefaultValue(null)]
        public new ImageList SmallImageList {
            get { return this.shadowedImageList; }
            set {
                this.shadowedImageList = value;
                if (this.UseSubItemCheckBoxes)
                {
                    this.SetupSubItemCheckBoxes();
                }

                this.SetupBaseImageList();
            }
        }
        private ImageList shadowedImageList;

        /// <summary>
        /// Return the size of the images in the small image list or a reasonable default
        /// </summary>
        [Browsable(false)]
        public virtual Size SmallImageSize {
            get {
                return this.BaseSmallImageList == null ? new Size(16, 16) : this.BaseSmallImageList.ImageSize;
            }
        }

        /// <summary>
        /// When the listview is grouped, how many pixels should exist between the end of one group and the
        /// beginning of the next?
        /// </summary>
        [Category("ObjectListView"),
         Description("How many pixels of space will be between groups"),
         DefaultValue(0)]
        public virtual int SpaceBetweenGroups {
            get { return this.spaceBetweenGroups; }
            set {
                if (this.spaceBetweenGroups == value)
                {
                    return;
                }

                this.spaceBetweenGroups = value;
                this.SetGroupSpacing();
            }
        }
        private int spaceBetweenGroups;

        private void SetGroupSpacing() {
            if (!this.IsHandleCreated)
            {
                return;
            }

            NativeMethods.LVGROUPMETRICS metrics = new NativeMethods.LVGROUPMETRICS();
            metrics.cbSize = (uint)Marshal.SizeOf<NativeMethods.LVGROUPMETRICS>();
            metrics.mask = (uint)GroupMetricsMask.LVGMF_BORDERSIZE;
            metrics.Bottom = (uint)this.SpaceBetweenGroups;
            NativeMethods.SetGroupMetrics(this, metrics);
        }

        /// <summary>
        /// Should the sort column show a slight tinge?
        /// </summary>
        [Category("ObjectListView"),
         Description("Should the sort column show a slight tinting?"),
         DefaultValue(false)]
        public virtual bool TintSortColumn {
            get { return this.tintSortColumn; }
            set {
                this.tintSortColumn = value;
                if (value && this.PrimarySortColumn != null)
                {
                    this.SelectedColumn = this.PrimarySortColumn;
                }
                else
                {
                    this.SelectedColumn = null;
                }
            }
        }
        private bool tintSortColumn;

        /// <summary>
        /// Should each row have a tri-state checkbox?
        /// </summary>
        /// <remarks>
        /// If this is true, the user can choose the third state (normally Indeterminate). Otherwise, user clicks
        /// alternate between checked and unchecked. CheckStateGetter can still return Indeterminate when this
        /// setting is false.
        /// </remarks>
        [Category("ObjectListView"),
         Description("Should the primary column have a checkbox that behaves as a tri-state checkbox?"),
         DefaultValue(false)]
        public virtual bool TriStateCheckBoxes {
            get { return triStateCheckBoxes; }
            set {
                triStateCheckBoxes = value;
                if (value && !this.CheckBoxes)
                {
                    this.CheckBoxes = true;
                }

                this.InitializeStateImageList();
            }
        }
        private bool triStateCheckBoxes;

        /// <summary>
        /// Get or set the index of the top item of this listview
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property only works when the listview is in Details view and not showing groups.
        /// </para>
        /// <para>
        /// The reason that it does not work when showing groups is that, when groups are enabled,
        /// the Windows msg LVM_GETTOPINDEX always returns 0, regardless of the
        /// scroll position.
        /// </para>
        /// </remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual int TopItemIndex {
            get {
                if (this.View == View.Details && this.IsHandleCreated)
                {
                    return NativeMethods.GetTopIndex(this);
                }

                return -1;
            }
            set {
                int newTopIndex = Math.Min(value, this.GetItemCount() - 1);
                if (this.View != View.Details || newTopIndex < 0)
                {
                    return;
                }

                try {
                    this.TopItem = this.Items[newTopIndex];

                    // Setting the TopItem sometimes gives off by one errors,
                    // that (bizarrely) are correct on a second attempt
                    if (this.TopItem != null && this.TopItem.Index != newTopIndex)
                    {
                        this.TopItem = this.GetItem(newTopIndex);
                    }
                }
                catch (NullReferenceException) {
                    // There is a bug in the .NET code where setting the TopItem
                    // will sometimes throw null reference exceptions
                    // There is nothing we can do to get around it.
                }
            }
        }

        /// <summary>
        /// Gets or sets whether moving the mouse over the header will trigger CellOver events.
        /// Defaults to true.
        /// </summary>
        /// <remarks>
        /// Moving the mouse over the header did not previously trigger CellOver events, since the
        /// header is considered a separate control. 
        /// If this change in behaviour causes your application problems, set this to false.
        /// If you are interested in knowing when the mouse moves over the header, set this property to true (the default).
        /// </remarks>
        [Category("ObjectListView"),
         Description("Should moving the mouse over the header trigger CellOver events?"),
         DefaultValue(true)]
        public bool TriggerCellOverEventsWhenOverHeader
        {
            get { return triggerCellOverEventsWhenOverHeader; }
            set { triggerCellOverEventsWhenOverHeader = value; }
        }
        private bool triggerCellOverEventsWhenOverHeader = true;

        /// <summary>
        /// When resizing a column by dragging its divider, should any space filling columns be
        /// resized at each mouse move? If this is false, the filling columns will be
        /// updated when the mouse is released.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If you have a space filling column
        /// is in the left of the column that is being resized, this will look odd: 
        /// the right edge of the column will be dragged, but
        /// its <b>left</b> edge will move since the space filling column is shrinking.
        /// </para>
        /// <para>This is logical behaviour -- it just looks wrong.   
        /// </para>
        /// <para>
        /// Given the above behavior is probably best to turn this property off if your space filling
        /// columns aren't the right-most columns.</para>
        /// </remarks>
        [Category("ObjectListView"),
        Description("When resizing a column by dragging its divider, should any space filling columns be resized at each mouse move?"),
        DefaultValue(true)]
        public virtual bool UpdateSpaceFillingColumnsWhenDraggingColumnDivider {
            get { return updateSpaceFillingColumnsWhenDraggingColumnDivider; }
            set { updateSpaceFillingColumnsWhenDraggingColumnDivider = value; }
        }
        private bool updateSpaceFillingColumnsWhenDraggingColumnDivider = true;

        /// <summary>
        /// What color should be used for the background of selected rows when the control doesn't have the focus?
        /// </summary>
        [Category("ObjectListView"),
         Description("The background color of selected rows when the control doesn't have the focus"),
         DefaultValue(typeof(Color), "")]
        public virtual Color UnfocusedSelectedBackColor {
            get { return this.unfocusedSelectedBackColor; }
            set { this.unfocusedSelectedBackColor = value; }
        }
        private Color unfocusedSelectedBackColor = Color.Empty;

        /// <summary>
        /// Return the color should be used for the background of selected rows when the control doesn't have the focus or a reasonable default
        /// </summary>
        [Browsable(false)]
        public virtual Color UnfocusedSelectedBackColorOrDefault {
            get {
                return this.UnfocusedSelectedBackColor.IsEmpty ? ObjectListViewTheme.Current.InactiveSelectionColor : this.UnfocusedSelectedBackColor;
            }
        }

        /// <summary>
        /// What color should be used for the foreground of selected rows when the control doesn't have the focus?
        /// </summary>
        [Category("ObjectListView"),
         Description("The foreground color of selected rows when the control is owner drawn and doesn't have the focus"),
         DefaultValue(typeof(Color), "")]
        public virtual Color UnfocusedSelectedForeColor {
            get { return this.unfocusedSelectedForeColor; }
            set { this.unfocusedSelectedForeColor = value; }
        }
        private Color unfocusedSelectedForeColor = Color.Empty;

        /// <summary>
        /// Return the color should be used for the foreground of selected rows when the control doesn't have the focus or a reasonable default
        /// </summary>
        [Browsable(false)]
        public virtual Color UnfocusedSelectedForeColorOrDefault {
            get {
                return this.UnfocusedSelectedForeColor.IsEmpty ? ObjectListViewTheme.Current.InactiveSelectionTextColor : this.UnfocusedSelectedForeColor;
            }
        }

        /// <summary>
        /// Gets or sets whether the list give a different background color to every second row? Defaults to false.
        /// </summary>
        /// <remarks><para>The color of the alternate rows is given by AlternateRowBackColor.</para>
        /// <para>There is a "feature" in .NET for listviews in non-full-row-select mode, where
        /// selected rows are not drawn with their correct background color.</para></remarks>
        [Category("ObjectListView"),
         Description("Should the list view use a different backcolor to alternate rows?"),
         DefaultValue(false)]
        public virtual bool UseAlternatingBackColors {
            get { return useAlternatingBackColors; }
            set { useAlternatingBackColors = value; }
        }
        private bool useAlternatingBackColors;

        /// <summary>
        /// Should FormatCell events be called for each cell in the control?
        /// </summary>
        /// <remarks>
        /// <para>In many situations, no cell level formatting is performed. ObjectListView
        /// can run somewhat faster if it does not trigger a format cell event for every cell
        /// unless it is required. So, by default, it does not raise an event for each cell.
        /// </para>
        /// <para>ObjectListView *does* raise a FormatRow event every time a row is rebuilt.
        /// Individual rows can decide whether to raise FormatCell
        /// events for every cell in row.
        /// </para>
        /// <para>
        /// Regardless of this setting, FormatCell events are only raised when the ObjectListView
        /// is in Details view.</para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("Should FormatCell events be triggered to every cell that is built?"),
         DefaultValue(false)]
        public bool UseCellFormatEvents {
            get { return useCellFormatEvents; }
            set { useCellFormatEvents = value; }
        }
        private bool useCellFormatEvents;

        /// <summary>
        /// Gets or sets whether this ObjectListView will use the same hot item and selection 
        /// mechanism that Vista Explorer does.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property has many imperfections:
        /// <list type="bullet">
        /// <item><description>This only works on Vista and later</description></item>
        /// <item><description>It does not work well with AlternateRowBackColors.</description></item>
        /// <item><description>It does not play well with HotItemStyles.</description></item>
        /// <item><description>It looks a little bit silly is FullRowSelect is false.</description></item>
        /// <item><description>It doesn't work at all when the list is owner drawn (since the renderers
        /// do all the drawing). As such, it won't work with TreeListView's since they *have to be*
        /// owner drawn. You can still set it, but it's just not going to be happy.</description></item>
        /// </list>
        /// But if you absolutely have to look like Vista/Win7, this is your property. 
        /// Do not complain if settings this messes up other things.
        /// </para>
        /// <para>
        /// When this property is set to true, the ObjectListView will be not owner drawn. This will
        /// disable many of the pretty drawing-based features of ObjectListView. 
        /// </para>
        /// <para>Because of the above, this property should never be set to true for TreeListViews,
        /// since they *require* owner drawing to be rendered correctly.</para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("Should the list use the same hot item and selection mechanism as Vista?"),
         DefaultValue(false)]
        public bool UseExplorerTheme {
            get { return useExplorerTheme; }
            set {
                useExplorerTheme = value;
                if (this.IsHandleCreated) {
                    ImageList stateImages = this.StateImageList;
                    try {
                        // Theme changes replace the native checkbox list; protect the managed image-list handle.
                        if (stateImages != null)
                        {
                            NativeMethods.SetStateImageList(this, null);
                        }
                        int result = NativeMethods.SetWindowTheme(this.Handle,
                            ObjectListViewTheme.IsDarkMode ? "DarkMode_Explorer" : value ? "explorer" : "", null);
                        if (result < 0)
                        {
                            Trace.TraceWarning("ObjectListView could not set its native theme (HRESULT {0:X8}).", result);
                        }
                        IntPtr header = NativeMethods.GetHeaderControl(this);
                        if (header != IntPtr.Zero)
                        {
                            result = NativeMethods.SetWindowTheme(header, ObjectListViewTheme.IsDarkMode ? "DarkMode_ItemsView" : null, null);
                            if (result < 0)
                            {
                                Trace.TraceWarning("ObjectListView could not set its header theme (HRESULT {0:X8}).", result);
                            }
                        }
                        NativeMethods.SetListViewColors(this);
                    } finally {
                        if (stateImages != null)
                        {
                            NativeMethods.SetStateImageList(this, stateImages);
                        }
                    }
                }
                
                this.OwnerDraw = !value;
            }
        }
        private bool useExplorerTheme;

        /// <summary>
        /// Reapplies the shared palette to this list without theming its containing form.
        /// </summary>
        /// <remarks>
        /// Call on the control thread after changing <see cref="ObjectListViewTheme"/> settings.
        /// Preserves explicit colors, renderers, owner drawing, selection, and model objects.
        /// Does not change the application's WinForms color mode.
        /// </remarks>
        public virtual void RefreshTheme() {
            if (ReferenceEquals(this.EmptyListMsgOverlay, this.standardEmptyListMsgOverlay))
            {
                IThemeColors colors = ObjectListViewTheme.Current;
                this.standardEmptyListMsgOverlay?.UpdateDefaultColors(
                    colors.EmptyListTextColor, colors.EmptyListBackgroundColor, colors.ControlDarkColor);
            }
            bool ownerDraw = this.OwnerDraw;
            try {
#pragma warning disable CA2245 // Reapply native styling without changing the configured behavior.
                this.UseExplorerTheme = this.UseExplorerTheme;
#pragma warning restore CA2245
            } finally {
                this.OwnerDraw = ownerDraw;
            }
            this.ClearCachedInfo();
            if (!this.VirtualMode)
            {
                foreach (OLVListItem item in this.Items)
                {
                    bool tracked = this.rowThemeColors.TryGetValue(item, out RowThemeColors previous);
                    foreach (ListViewItem.ListViewSubItem cell in item.SubItems)
                    {
                        if (!tracked || cell.BackColor.ToArgb() == previous.BackColor.ToArgb())
                        {
                            cell.BackColor = this.BackColor;
                        }
                        if (!tracked || cell.ForeColor.ToArgb() == previous.ForeColor.ToArgb())
                        {
                            cell.ForeColor = this.ForeColor;
                        }
                    }
                    var defaults = this.rowThemeColors.GetOrCreateValue(item);
                    defaults.BackColor = this.BackColor;
                    defaults.ForeColor = this.ForeColor;
                    if (!item.Enabled)
                    {
                        ApplyRowStyle(item, this.DisabledItemStyle ?? ObjectListViewBase.DefaultDisabledItemStyle);
                    }
                    this.RowFormatter?.Invoke(item);
                }
                this.PostProcessRows();
                if ((this.UseHotItem || this.UseHyperlinks) && this.HotRowIndex >= 0
                    && this.HotRowIndex < this.GetItemCount())
                {
                    this.UpdateHotRow(this.GetItem(this.HotRowIndex));
                }
            }
            if (this.IsHandleCreated)
            {
                this.HeaderControl.Invalidate();
                this.UpdateScrollBarGlass(NativeListViewTheme.ShouldPaint);
            }
            this.Invalidate(true);
        }

        /// <summary>
        /// Gets or sets whether the list should enable filtering
        /// </summary>
        [Category("ObjectListView"),
        Description("Should the list enable filtering?"),
        DefaultValue(false)]
        public virtual bool UseFiltering {
            get { return useFiltering; }
            set {
                if (useFiltering == value)
                {
                    return;
                }

                useFiltering = value;
                this.UpdateFiltering();
            }
        }
        private bool useFiltering;

        /// <summary>
        /// Gets or sets whether the list should put an indicator into a column's header to show that
        /// it is filtering on that column
        /// </summary>
        /// <remarks>If you set this to true, HeaderUsesThemes is automatically set to false, since
        /// we can only draw a filter indicator when not using a themed header.</remarks>
        [Category("ObjectListView"),
        Description("Should an image be drawn in a column's header when that column is being used for filtering?"),
        DefaultValue(false)]
        public virtual bool UseFilterIndicator {
            get { return useFilterIndicator; }
            set {
                if (this.useFilterIndicator == value)
                {
                    return;
                }

                useFilterIndicator = value;
                if (this.useFilterIndicator)
                {
                    this.HeaderUsesThemes = false;
                }

                this.Invalidate();
            }
        }
        private bool useFilterIndicator;

        /// <summary>
        /// Should controls (checkboxes or buttons) that are under the mouse be drawn "hot"?
        /// </summary>
        /// <remarks>
        /// <para>If this is false, control will not be drawn differently when the mouse is over them.</para>
        /// <para>
        /// If this is false AND UseHotItem is false AND UseHyperlinks is false, then the ObjectListView
        /// can skip some processing on mouse move. This make mouse move processing use almost no CPU.
        /// </para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("Should controls (checkboxes or buttons) that are under the mouse be drawn hot?"),
         DefaultValue(true)]
        public bool UseHotControls {
            get { return this.useHotControls; }
            set { this.useHotControls = value; }
        }
        private bool useHotControls = true;

        /// <summary>
        /// Should the item under the cursor be formatted in a special way?
        /// </summary>
        [Category("ObjectListView"),
         Description("Should HotTracking be used? Hot tracking applies special formatting to the row under the cursor"),
         DefaultValue(false)]
        public bool UseHotItem {
            get { return this.useHotItem; }
            set {
                this.useHotItem = value;
                if (value)
                {
                    this.AddOverlay(this.HotItemStyleOrDefault.Overlay);
                }
                else
                {
                    this.RemoveOverlay(this.HotItemStyleOrDefault.Overlay);
                }
            }
        }
        private bool useHotItem;

        /// <summary>
        /// Gets or sets whether this listview should show hyperlinks in the cells.
        /// </summary>
        [Category("ObjectListView"),
         Description("Should hyperlinks be shown on this control?"),
         DefaultValue(false)]
        public bool UseHyperlinks {
            get { return this.useHyperlinks; }
            set {
                this.useHyperlinks = value;
                if (value && this.HyperlinkStyle == null)
                {
                    this.HyperlinkStyle = new HyperlinkStyle();
                }
            }
        }
        private bool useHyperlinks;

        /// <summary>
        /// Should this control show overlays
        /// </summary>
        /// <remarks>Overlays are enabled by default and would only need to be disabled
        /// if they were causing problems in your development environment.</remarks>
        [Category("ObjectListView"),
         Description("Should this control show overlays"),
         DefaultValue(true)]
        public bool UseOverlays {
            get { return this.useOverlays; }
            set { this.useOverlays = value; }
        }
        private bool useOverlays = true;

        /// <summary>
        /// Should this control be configured to show check boxes on subitems?
        /// </summary>
        /// <remarks>If this is set to True, the control will be given a SmallImageList if it
        /// doesn't already have one. Also, if it is a virtual list, it will be set to owner
        /// drawn, since virtual lists can't draw check boxes without being owner drawn.</remarks>
        [Category("ObjectListView"),
         Description("Should this control be configured to show check boxes on subitems."),
         DefaultValue(false)]
        public bool UseSubItemCheckBoxes {
            get { return this.useSubItemCheckBoxes; }
            set {
                this.useSubItemCheckBoxes = value;
                if (value)
                {
                    this.SetupSubItemCheckBoxes();
                }
            }
        }
        private bool useSubItemCheckBoxes;

        /// <summary>
        /// Gets or sets if the ObjectListView will use a translucent selection mechanism like Vista.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Unlike UseExplorerTheme, this Vista-like scheme works on XP and for both
        /// owner and non-owner drawn lists.
        /// </para>
        /// <para>
        /// This will replace any SelectedRowDecoration that has been installed.
        /// </para>
        /// <para>
        /// If you don't like the colours used for the selection, ignore this property and 
        /// just create your own RowBorderDecoration and assigned it to SelectedRowDecoration,
        /// just like this property setter does.
        /// </para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("Should the list use a translucent selection mechanism (like Vista)"),
         DefaultValue(false)]
        public bool UseTranslucentSelection {
            get { return useTranslucentSelection; }
            set {
                useTranslucentSelection = value;
                if (value) {
                    RowBorderDecoration rbd = new RowBorderDecoration();
                    rbd.BorderPen = new Pen(Color.FromArgb(154, 223, 251));
                    rbd.FillBrush = new SolidBrush(Color.FromArgb(48, 163, 217, 225));
                    rbd.BoundsPadding = new Size(0, 0);
                    rbd.CornerRounding = 6.0f;
                    this.SelectedRowDecoration = rbd;
                } else
                {
                    this.SelectedRowDecoration = null;
                }
            }
        }
        private bool useTranslucentSelection;
        
        /// <summary>
        /// Gets or sets if the ObjectListView will use a translucent hot row highlighting mechanism like Vista.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Setting this will replace any HotItemStyle that has been installed.
        /// </para>
        /// <para>
        /// If you don't like the colours used for the hot item, ignore this property and 
        /// just create your own HotItemStyle, fill in the values you want, and assigned it to HotItemStyle property,
        /// just like this property setter does.
        /// </para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("Should the list use a translucent hot row highlighting mechanism (like Vista)"),
         DefaultValue(false)]
        public bool UseTranslucentHotItem {
            get { return useTranslucentHotItem; }
            set {
                useTranslucentHotItem = value;
                if (value) {
                    RowBorderDecoration rbd = new RowBorderDecoration();
                    rbd.BorderPen = new Pen(Color.FromArgb(154, 223, 251));
                    rbd.BoundsPadding = new Size(0, 0);
                    rbd.CornerRounding = 6.0f;
                    rbd.FillGradientFrom = Color.FromArgb(0, 255, 255, 255);
                    rbd.FillGradientTo = Color.FromArgb(64, 183, 237, 240);
                    HotItemStyle his = new HotItemStyle();
                    his.Decoration = rbd;
                    this.HotItemStyle = his;
                } else
                {
                    this.HotItemStyle = null;
                }

                this.UseHotItem = value;
            }
        }
        private bool useTranslucentHotItem;

        /// <summary>
        /// Get/set the style of view that this listview is using
        /// </summary>
        /// <remarks>Switching to tile or details view installs the columns appropriate to that view.
        /// Confusingly, in tile view, every column is shown as a row of information.</remarks>
        [Category("Appearance"),
         Description("Select the layout of the items within this control)"),
         DefaultValue(null)]
        public new View View
        {
            get { return base.View; }
            set {
                if (base.View == value)
                {
                    return;
                }

                if (this.Frozen) {
                    base.View = value;
                    this.SetupBaseImageList();
                } else {
                    this.Freeze();

                    if (value == View.Tile)
                    {
                        this.CalculateReasonableTileSize();
                    }

                    base.View = value;
                    this.SetupBaseImageList();
                    this.Unfreeze();
                }
            }
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// This delegate fetches the checkedness of an object as a boolean only.
        /// </summary>
        /// <remarks>Use this if you never want to worry about the
        /// Indeterminate state (which is fairly common).
        /// <para>
        /// This is a convenience wrapper around the CheckStateGetter property.
        /// </para>
        /// </remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual BooleanCheckStateGetterDelegate BooleanCheckStateGetter {
            set {
                if (value == null)
                {
                    this.CheckStateGetter = null;
                }
                else
                {
                    this.CheckStateGetter = delegate(Object x) {
                        return value(x) ? CheckState.Checked : CheckState.Unchecked;
                    };
                }
            }
        }

        /// <summary>
        /// This delegate sets the checkedness of an object as a boolean only. It must return
        /// true or false indicating if the object was checked or not.
        /// </summary>
        /// <remarks>Use this if you never want to worry about the
        /// Indeterminate state (which is fairly common).
        /// <para>
        /// This is a convenience wrapper around the CheckStatePutter property.
        /// </para>
        /// </remarks>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual BooleanCheckStatePutterDelegate BooleanCheckStatePutter {
            set {
                if (value == null)
                {
                    this.CheckStatePutter = null;
                }
                else
                {
                    this.CheckStatePutter = delegate(Object x, CheckState state) {
                        bool isChecked = (state == CheckState.Checked);
                        return value(x, isChecked) ? CheckState.Checked : CheckState.Unchecked;
                    };
                }
            }
        }

        /// <summary>
        /// Gets whether or not this listview is capable of showing groups
        /// </summary>
        [Browsable(false)]
        public virtual bool CanShowGroups {
            get {
                return true;
            }
        }

        /// <summary>
        /// Gets or sets whether ObjectListView can rely on Application.Idle events
        /// being raised.
        /// </summary>
        /// <remarks>In some host environments (e.g. when running as an extension within
        /// VisualStudio and possibly Office), Application.Idle events are never raised.
        /// Set this to false when Idle events will not be raised, and ObjectListView will
        /// raise those events itself.
        /// </remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual bool CanUseApplicationIdle {
            get { return this.canUseApplicationIdle; }
            set { this.canUseApplicationIdle = value; }
        }
        private bool canUseApplicationIdle = true;

        /// <summary>
        /// This delegate fetches the renderer for a particular cell. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this returns null (or is not installed), the renderer for the column will be used.
        /// If the column renderer is null, then <seealso cref="DefaultRenderer"/> will be used.
        /// </para>
        /// <para>
        /// This is called every time any cell is drawn. It must be efficient! 
        /// </para>
        /// </remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual CellRendererGetterDelegate CellRendererGetter
        {
            get { return this.cellRendererGetter; }
            set { this.cellRendererGetter = value; }
        }
        private CellRendererGetterDelegate cellRendererGetter;

        /// <summary>
        /// This delegate is called when the list wants to show a tooltip for a particular cell.
        /// The delegate should return the text to display, or null to use the default behavior
        /// (which is to show the full text of truncated cell values).
        /// </summary>
        /// <remarks>
        /// Displaying the full text of truncated cell values only work for FullRowSelect listviews.
        /// This is MS's behavior, not mine. Don't complain to me :)
        /// </remarks>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual CellToolTipGetterDelegate CellToolTipGetter {
            get { return cellToolTipGetter; }
            set { cellToolTipGetter = value; }
        }
        private CellToolTipGetterDelegate cellToolTipGetter;

        /// <summary>
        /// The name of the property (or field) that holds whether or not a model is checked.
        /// </summary>
        /// <remarks>
        /// <para>The property be modifiable. It must have a return type of bool (or of bool? if
        /// TriStateCheckBoxes is true).</para>
        /// <para>Setting this property replaces any CheckStateGetter or CheckStatePutter that have been installed.
        /// Conversely, later setting the CheckStateGetter or CheckStatePutter properties will take precedence
        /// over the behavior of this property.</para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("The name of the property or field that holds the 'checkedness' of the model"),
         DefaultValue(null)]
        public virtual string CheckedAspectName {
            get { return checkedAspectName; }
            set {
                checkedAspectName = value;
                if (String.IsNullOrEmpty(checkedAspectName)) {
                    this.checkedAspectMunger = null;
                    this.CheckStateGetter = null;
                    this.CheckStatePutter = null;
                } else {
                    this.checkedAspectMunger = new Munger(checkedAspectName);
                    this.CheckStateGetter = delegate(Object modelObject) {
                        bool? result = this.checkedAspectMunger.GetValue(modelObject) as bool?;
                        if (result.HasValue)
                        {
                            return result.Value ? CheckState.Checked : CheckState.Unchecked;
                        }

                        return this.TriStateCheckBoxes ? CheckState.Indeterminate : CheckState.Unchecked;
                    };
                    this.CheckStatePutter = delegate(Object modelObject, CheckState newValue) {
                        if (this.TriStateCheckBoxes && newValue == CheckState.Indeterminate)
                        {
                            this.checkedAspectMunger.PutValue(modelObject, null);
                        }
                        else
                        {
                            this.checkedAspectMunger.PutValue(modelObject, newValue == CheckState.Checked);
                        }

                        return this.CheckStateGetter(modelObject);
                    };
                }
            }
        }
        private string checkedAspectName;
        private Munger checkedAspectMunger;

        /// <summary>
        /// This delegate will be called whenever the ObjectListView needs to know the check state
        /// of the row associated with a given model object.
        /// </summary>
        /// <remarks>
        /// <para>.NET has no support for indeterminate values, but as of v2.0, this class allows
        /// indeterminate values.</para>
        /// </remarks>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual CheckStateGetterDelegate CheckStateGetter {
            get { return checkStateGetter; }
            set { checkStateGetter = value; }
        }
        private CheckStateGetterDelegate checkStateGetter;

        /// <summary>
        /// This delegate will be called whenever the user tries to change the check state of a row.
        /// The delegate should return the state that was actually set, which may be different
        /// to the state given.
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual CheckStatePutterDelegate CheckStatePutter {
            get { return checkStatePutter; }
            set { checkStatePutter = value; }
        }
        private CheckStatePutterDelegate checkStatePutter;

        /// <summary>
        /// This delegate can be used to sort the table in a custom fashion.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The delegate must install a ListViewItemSorter on the ObjectListView.
        /// Installing the ItemSorter does the actual work of sorting the ListViewItems.
        /// See ColumnComparer in the code for an example of what an ItemSorter has to do.
        /// </para>
        /// <para>
        /// Do not install a CustomSorter on a VirtualObjectListView. Override the SortObjects()
        /// method of the IVirtualListDataSource instead.
        /// </para>
        /// </remarks>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual SortDelegate CustomSorter {
            get { return customSorter; }
            set { customSorter = value; }
        }
        private SortDelegate customSorter;

        /// <summary>
        /// This delegate is called when the list wants to show a tooltip for a particular header.
        /// The delegate should return the text to display, or null to use the default behavior
        /// (which is to not show any tooltip).
        /// </summary>
        /// <remarks>
        /// Installing a HeaderToolTipGetter takes precedence over any text in OLVColumn.ToolTipText.
        /// </remarks>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual HeaderToolTipGetterDelegate HeaderToolTipGetter {
            get { return headerToolTipGetter; }
            set { headerToolTipGetter = value; }
        }
        private HeaderToolTipGetterDelegate headerToolTipGetter;

        /// <summary>
        /// This delegate can be used to format a OLVListItem before it is added to the control.
        /// </summary>
        /// <remarks>
        /// <para>The model object for the row can be found through the RowObject property of the OLVListItem object.</para>
        /// <para>All subitems normally have the same style as list item, so setting the forecolor on one
        /// subitem changes the forecolor of all subitems.
        /// To allow subitems to have different attributes, do this:
        /// <code>myListViewItem.UseItemStyleForSubItems = false;</code>.
        /// </para>
        /// <para>If UseAlternatingBackColors is true, the backcolor of the listitem will be calculated
        /// by the control and cannot be controlled by the RowFormatter delegate.
        /// In general, trying to use a RowFormatter
        /// when UseAlternatingBackColors is true does not work well.</para>
        /// <para>As it says in the summary, this is called <b>before</b> the item is added to the control.
        /// Many properties of the OLVListItem itself are not available at that point, including:
        /// Index, Selected, Focused, Bounds, Checked, DisplayIndex.</para>
        /// </remarks>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual RowFormatterDelegate RowFormatter {
            get { return rowFormatter; }
            set { rowFormatter = value; }
        }
        private RowFormatterDelegate rowFormatter;

        #endregion

        #region Utilities

        private static CheckState CalculateToggledCheckState(CheckState currentState, bool isTriState, bool isDisabled)
        {
            if (isDisabled)
            {
                return currentState;
            }

            switch (currentState)
            {
                case CheckState.Checked: return isTriState ? CheckState.Indeterminate : CheckState.Unchecked;
                case CheckState.Indeterminate: return CheckState.Unchecked;
                default: return CheckState.Checked;
            }
        }

        /// <summary>
        /// Do the actual work of creating the given list of groups
        /// </summary>
        /// <param name="groups"></param>
        protected virtual void CreateGroups(IEnumerable<OLVGroup> groups) {
            this.Groups.Clear();
            // The group must be added before it is given items, otherwise an exception is thrown (is this documented?)
            foreach (OLVGroup group in groups) {
                group.InsertGroupOldStyle(this);
                group.SetItemsOldStyle();
            }
        }

        /// <summary>
        /// For some reason, UseItemStyleForSubItems doesn't work for the colors
        /// when owner drawing the list, so we have to specifically give each subitem
        /// the desired colors
        /// </summary>
        /// <param name="olvi">The item whose subitems are to be corrected</param>
        /// <remarks>Cells drawn via BaseRenderer don't need this, but it is needed
        /// when an owner drawn cell uses DrawDefault=true</remarks>
        protected virtual void CorrectSubItemColors(ListViewItem olvi) {
        }

        /// <summary>
        /// Fill in the given OLVListItem with values of the given row
        /// </summary>
        /// <param name="lvi">the OLVListItem that is to be stuff with values</param>
        /// <param name="rowObject">the model object from which values will be taken</param>
        protected virtual void FillInValues(OLVListItem lvi, object rowObject) {
            if (this.Columns.Count == 0)
            {
                return;
            }

            OLVListSubItem subItem = this.MakeSubItem(rowObject, this.GetColumn(0));
            lvi.SubItems[0] = subItem;
            lvi.ImageSelector = subItem.ImageSelector;

            // Give the item the same font/colors as the control
            lvi.Font = this.Font;
            Color backColor = this.BackColor;
            Color foreColor = this.ForeColor;
            lvi.BackColor = backColor;
            lvi.ForeColor = foreColor;
            if (!this.VirtualMode) {
                var defaults = this.rowThemeColors.GetOrCreateValue(lvi);
                defaults.BackColor = backColor;
                defaults.ForeColor = foreColor;
            }

            // Should the row be selectable?
            lvi.Enabled = !this.IsDisabled(rowObject);

            // Only Details and Tile views have subitems
            switch (this.View) {
                case View.Details:
                    for (int i = 1; i < this.Columns.Count; i++) {
                        lvi.SubItems.Add(this.MakeSubItem(rowObject, this.GetColumn(i)));
                    }
                    break;
                case View.Tile:
                    for (int i = 1; i < this.Columns.Count; i++) {
                        OLVColumn column = this.GetColumn(i);
                        if (column.IsTileViewColumn)
                        {
                            lvi.SubItems.Add(this.MakeSubItem(rowObject, column));
                        }
                    }
                    break;
            }

            // Should the row be selectable?
            if (!lvi.Enabled) {
                lvi.UseItemStyleForSubItems = false;
                ApplyRowStyle(lvi, this.DisabledItemStyle ?? ObjectListViewBase.DefaultDisabledItemStyle);
            }

            // Set the check state of the row, if we are showing check boxes
            if (this.CheckBoxes) {
                CheckState? state = this.GetCheckState(lvi.RowObject);
                if (state.HasValue)
                {
                    lvi.CheckState = state.Value;
                }
            }

            // Give the RowFormatter a chance to mess with the item
            if (this.RowFormatter != null) {
                this.RowFormatter(lvi);
            }
        }

        private OLVListSubItem MakeSubItem(object rowObject, OLVColumn column) {
            object cellValue = column.GetValue(rowObject);
            OLVListSubItem subItem = new OLVListSubItem(cellValue,
                                                        column.ValueToString(cellValue),
                                                        column.GetImage(rowObject));
            if (this.UseHyperlinks && column.Hyperlink) {
                IsHyperlinkEventArgs args = new IsHyperlinkEventArgs();
                args.ListView = this;
                args.Model = rowObject;
                args.Column = column;
                args.Text = subItem.Text;
                args.Url = subItem.Text;
                args.IsHyperlink = !this.IsDisabled(rowObject);
                this.OnIsHyperlink(args);
                subItem.Url = args.IsHyperlink ? args.Url : null;
            }

            return subItem;
        }

        private void ApplyHyperlinkStyle(OLVListItem olvi) {

            for (int i = 0; i < this.Columns.Count; i++) {
                OLVListSubItem subItem = olvi.GetSubItem(i);
                if (subItem == null)
                {
                    continue;
                }

                OLVColumn column = this.GetColumn(i);
                if (column.Hyperlink && !String.IsNullOrEmpty(subItem.Url))
                {
                    this.ApplyCellStyle(olvi, i, this.IsUrlVisited(subItem.Url) ? this.HyperlinkStyle.Visited : this.HyperlinkStyle.Normal);
                }
            }
        }


        /// <summary>
        /// Make sure the ListView has the extended style that says to display subitem images.
        /// </summary>
        /// <remarks>This method must be called after any .NET call that update the extended styles
        /// since they seem to erase this setting.</remarks>
        protected virtual void ForceSubItemImagesExStyle() {
            // Virtual lists can't show subitem images natively, so don't turn on this flag
            if (!this.VirtualMode)
            {
                NativeMethods.ForceSubItemImagesExStyle(this);
            }
        }

        /// <summary>
        /// Convert the given image selector to an index into our image list.
        /// Return -1 if that's not possible
        /// </summary>
        /// <param name="imageSelector"></param>
        /// <returns>Index of the image in the imageList, or -1</returns>
        protected virtual int GetActualImageIndex(Object imageSelector) {
            if (imageSelector == null)
            {
                return -1;
            }

            if (imageSelector is Int32)
            {
                return (int)imageSelector;
            }

            String imageSelectorAsString = imageSelector as String;
            if (imageSelectorAsString != null && this.SmallImageList != null)
            {
                return this.SmallImageList.Images.IndexOfKey(imageSelectorAsString);
            }

            return -1;
        }

        /// <summary>
        /// Return the tooltip that should be shown when the mouse is hovered over the given column
        /// </summary>
        /// <param name="columnIndex">The column index whose tool tip is to be fetched</param>
        /// <returns>A string or null if no tool tip is to be shown</returns>
        public virtual String GetHeaderToolTip(int columnIndex) {
            OLVColumn column = this.GetColumn(columnIndex);
            if (column == null)
            {
                return null;
            }

            String tooltip = column.ToolTipText;
            if (this.HeaderToolTipGetter != null)
            {
                tooltip = this.HeaderToolTipGetter(column);
            }

            return tooltip;
        }

        /// <summary>
        /// Return the tooltip that should be shown when the mouse is hovered over the given cell
        /// </summary>
        /// <param name="columnIndex">The column index whose tool tip is to be fetched</param>
        /// <param name="rowIndex">The row index whose tool tip is to be fetched</param>
        /// <returns>A string or null if no tool tip is to be shown</returns>
        public virtual String GetCellToolTip(int columnIndex, int rowIndex) {
            if (this.CellToolTipGetter != null)
            {
                return this.CellToolTipGetter(this.GetColumn(columnIndex), this.GetModelAt(rowIndex));
            }

            // Show the URL in the tooltip if it's different to the text
            if (columnIndex >= 0) {
                OLVListSubItem subItem = this.GetSubItem(rowIndex, columnIndex);
                if (subItem != null && !String.IsNullOrEmpty(subItem.Url) && subItem.Url != subItem.Text &&
                    this.HotCellHitLocation == HitTestLocation.Text)
                {
                    return subItem.Url;
                }
            }

            return null;
        }

        /// <summary>
        /// Return the OLVListItem that displays the given model object
        /// </summary>
        /// <param name="modelObject">The modelObject whose item is to be found</param>
        /// <returns>The OLVListItem that displays the model, or null</returns>
        /// <remarks>This method has O(n) performance.</remarks>
        public virtual OLVListItem ModelToItem(object modelObject) {
            if (modelObject == null)
            {
                return null;
            }

            foreach (OLVListItem olvi in this.Items) {
                if (olvi.RowObject != null && olvi.RowObject.Equals(modelObject))
                {
                    return olvi;
                }
            }
            return null;
        }

        /// <summary>
        /// Do the work required after the items in a listview have been created
        /// </summary>
        protected virtual void PostProcessRows() {
            // If this method is called during a BeginUpdate/EndUpdate pair, changes to the
            // Items collection are cached. Getting the Count flushes that cache.
#pragma warning disable 168
// ReSharper disable once UnusedVariable
            int count = this.Items.Count;
#pragma warning restore 168

            bool? previousHighContrast = this.rowStyleHighContrast;
            // Keep one accessibility sample for the synchronous formatting batch, including nested calls.
            this.rowStyleHighContrast = SystemInformation.HighContrast;
            try {
                int i = 0;
                if (this.ShowGroups && this.Groups.Count > 0) {
                    foreach (ListViewGroup group in this.Groups) {
                        foreach (OLVListItem olvi in group.Items) {
                            this.PostProcessOneRow(olvi.Index, i, olvi);
                            i++;
                        }
                    }
                } else {
                    foreach (OLVListItem olvi in this.Items) {
                        this.PostProcessOneRow(olvi.Index, i, olvi);
                        i++;
                    }
                }
            } finally {
                this.rowStyleHighContrast = previousHighContrast;
            }
        }

        /// <summary>
        /// Do the work required after one item in a listview have been created
        /// </summary>
        protected virtual void PostProcessOneRow(int rowIndex, int displayIndex, OLVListItem olvi) {
            if (this.Columns.Count == 0)
            {
                return;
            }

            if (this.UseAlternatingBackColors && this.View == View.Details && olvi.Enabled) {
                olvi.UseItemStyleForSubItems = true;
                olvi.BackColor = displayIndex % 2 == 1 ? this.AlternateRowBackColorOrDefault : this.BackColor;
                this.rowThemeColors.GetOrCreateValue(olvi).BackColor = olvi.BackColor;
            }
            if (this.ShowImagesOnSubItems && !this.VirtualMode)
            {
                this.SetSubItemImages(rowIndex, olvi);
            }

            bool needToTriggerFormatCellEvents = this.FormatRowWithAppearance(rowIndex, displayIndex, olvi);

            // We only need cell level events if we are in details view
            if (this.View != View.Details)
            {
                this.ApplyNativeStyleAccessibility(olvi);
                return;
            }

            // If we're going to have per cell formatting, we need to copy the formatting
            // of the item into each cell, before triggering the cell format events
            if (needToTriggerFormatCellEvents) {
                PropagateFormatFromRowToCells(olvi);
                this.TriggerFormatCellEvents(rowIndex, displayIndex, olvi);
            }

            // Similarly, if any cell in the row has hyperlinks, we have to copy formatting
            // from the item into each cell before applying the hyperlink style
            if (this.UseHyperlinks && olvi.HasAnyHyperlinks) {
                PropagateFormatFromRowToCells(olvi);
                this.ApplyHyperlinkStyle(olvi);
            }
            this.ApplyNativeStyleAccessibility(olvi);
        }

        /// <summary>
        /// Prepare the listview to show alternate row backcolors
        /// </summary>
        /// <remarks>We cannot rely on lvi.Index in this method.
        /// In a straight list, lvi.Index is the display index, and can be used to determine
        /// whether the row should be colored. But when organised by groups, lvi.Index is not
        /// usable because it still refers to the position in the overall list, not the display order.
        ///</remarks>
        [Obsolete("This method is no longer used. Override PostProcessOneRow() to achieve a similar result")]
        protected virtual void PrepareAlternateBackColors() {
        }

        /// <summary>
        /// Setup all subitem images on all rows
        /// </summary>
        [Obsolete("This method is not longer maintained and will be removed", false)]
        protected virtual void SetAllSubItemImages() {
            //if (!this.ShowImagesOnSubItems || this.OwnerDraw)
            //    return;

            //this.ForceSubItemImagesExStyle();

            //for (int rowIndex = 0; rowIndex < this.GetItemCount(); rowIndex++)
            //    SetSubItemImages(rowIndex, this.GetItem(rowIndex));
        }

        /// <summary>
        /// Tell the underlying list control which images to show against the subitems
        /// </summary>
        /// <param name="rowIndex">the index at which the item occurs</param>
        /// <param name="item">the item whose subitems are to be set</param>
        protected virtual void SetSubItemImages(int rowIndex, OLVListItem item) {
            this.SetSubItemImages(rowIndex, item, false);
        }

        /// <summary>
        /// Tell the underlying list control which images to show against the subitems
        /// </summary>
        /// <param name="rowIndex">the index at which the item occurs</param>
        /// <param name="item">the item whose subitems are to be set</param>
        /// <param name="shouldClearImages">will existing images be cleared if no new image is provided?</param>
        protected virtual void SetSubItemImages(int rowIndex, OLVListItem item, bool shouldClearImages) {
            if (!this.ShowImagesOnSubItems || this.OwnerDraw)
            {
                return;
            }

            for (int i = 1; i < item.SubItems.Count; i++) {
                this.SetSubItemImage(rowIndex, i, item.GetSubItem(i), shouldClearImages);
            }
        }

        /// <summary>
        /// Set the subitem image natively
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="subItemIndex"></param>
        /// <param name="subItem"></param>
        /// <param name="shouldClearImages"></param>
        public virtual void SetSubItemImage(int rowIndex, int subItemIndex, OLVListSubItem subItem, bool shouldClearImages) {
            int imageIndex = this.GetActualImageIndex(subItem.ImageSelector);
            if (shouldClearImages || imageIndex != -1)
            {
                NativeMethods.SetSubItemImage(this, rowIndex, subItemIndex, imageIndex);
            }
        }

        /// <summary>
        /// Ensure that model mutations operate on an owned collection.
        /// </summary>
        /// <remarks>
        /// Collections are now materialized on assignment. This hook is retained for subclasses.
        /// </remarks>
        protected virtual void TakeOwnershipOfObjects() {
        }

        /// <summary>
        /// Trigger FormatRow and possibly FormatCell events for the given item
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="displayIndex"></param>
        /// <param name="olvi"></param>
        protected virtual bool TriggerFormatRowEvent(int rowIndex, int displayIndex, OLVListItem olvi) {
            FormatRowEventArgs args = new FormatRowEventArgs();
            args.ListView = this;
            args.RowIndex = rowIndex;
            args.DisplayIndex = displayIndex;
            args.Item = olvi;
            args.UseCellFormatEvents = this.UseCellFormatEvents;
            this.OnFormatRow(args);
            return args.UseCellFormatEvents;
        }

        /// <summary>
        /// Trigger FormatCell events for the given item
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="displayIndex"></param>
        /// <param name="olvi"></param>
        protected virtual void TriggerFormatCellEvents(int rowIndex, int displayIndex, OLVListItem olvi) {

            PropagateFormatFromRowToCells(olvi);

            // Fire one event per cell
            FormatCellEventArgs args2 = new FormatCellEventArgs();
            args2.ListView = this;
            args2.RowIndex = rowIndex;
            args2.DisplayIndex = displayIndex;
            args2.Item = olvi;
            for (int i = 0; i < this.Columns.Count; i++) {
                args2.ColumnIndex = i;
                args2.Column = this.GetColumn(i);
                args2.SubItem = olvi.GetSubItem(i);
                this.OnFormatCell(args2);
            }
        }

        private static void PropagateFormatFromRowToCells(OLVListItem olvi) {
            // If a cell isn't given its own colors, it *should* use the colors of the item.
            // However, there is a bug in the .NET framework where the cell are given
            // the colors of the ListView instead of the colors of the row.

            // If we've already done this, don't do it again
            if (olvi.UseItemStyleForSubItems == false)
            {
                return;
            }

            // So we have to explicitly give each cell the fore and back colors and the font that it should have.
            olvi.UseItemStyleForSubItems = false;
            Color backColor = olvi.BackColor;
            Color foreColor = olvi.ForeColor;
            Font font = olvi.Font;
            foreach (ListViewItem.ListViewSubItem subitem in olvi.SubItems) {
                subitem.BackColor = backColor;
                subitem.ForeColor = foreColor;
                subitem.Font = font;
            }
        }

        /// <summary>
        /// Make the list forget everything -- all rows and all columns
        /// </summary>
        /// <remarks>Use <see cref="ClearObjects"/> if you want to remove just the rows.</remarks>
        public virtual void Reset() {
            this.Clear();
            this.AllColumns.Clear();
            this.ClearObjects();
            this.sortDescriptions.Clear();
            if (this.TintSortColumn)
            {
                this.SelectedColumn = null;
            }

            this.ClearDisabledObjects();
            this.ClearPersistentCheckState();
            this.ClearUrlVisited();
            this.ClearHotItem();
        }


        #endregion

        #region ISupportInitialize Members

        private bool initializingColumns;

        void ISupportInitialize.BeginInit() {
            this.initializingColumns = true;
            this.Frozen = true;
        }

        void ISupportInitialize.EndInit() {
            this.initializingColumns = false;
            if (this.RowHeight != -1) {
#pragma warning disable CA2245 // Apply the final designer image list with the configured row height.
                this.SmallImageList = this.SmallImageList;
#pragma warning restore CA2245
                if (this.CheckBoxes)
                {
                    this.InitializeStateImageList();
                }
            }

            if (this.UseSubItemCheckBoxes || (this.VirtualMode && this.CheckBoxes))
            {
                this.SetupSubItemCheckBoxes();
            }

            if (this.IsHandleCreated)
            {
                this.RescaleColumnWidthsForDpi(this.GetWindowDpi());
            }

            this.Frozen = false;
        }

        #endregion

        #region Owner drawing

        /// <summary>
        /// Owner draw the column header
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e) {
            e.DrawDefault = true;
            base.OnDrawColumnHeader(e);
        }

        /// <summary>
        /// Owner draw the item
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDrawItem(DrawListViewItemEventArgs e) {
            if (this.View == View.Details)
            {
                e.DrawDefault = false;
            }
            else {
                if (this.ItemRenderer == null)
                {
                    e.DrawDefault = true;
                }
                else {
                    Object row = ((OLVListItem)e.Item).RowObject;
                    e.DrawDefault = !this.ItemRenderer.RenderItem(e, e.Graphics, e.Bounds, row);
                }
            }

            if (e.DrawDefault)
            {
                base.OnDrawItem(e);
            }
        }

        /// <summary>
        /// Owner draw a single subitem
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e) {
            //System.Diagnostics.Debug.WriteLine(String.Format("OnDrawSubItem ({0}, {1})", e.ItemIndex, e.ColumnIndex));
            // Don't try to do owner drawing at design time
            if (this.DesignMode) {
                e.DrawDefault = true;
                return;
            }

            object rowObject = ((OLVListItem)e.Item).RowObject;

            // Calculate where the subitem should be drawn
            Rectangle r = ((OLVListItem)e.Item).GetSubItemBounds(e.ColumnIndex);

            // Get the special renderer for this column. If there isn't one, use the default draw mechanism.
            OLVColumn column = this.GetColumn(e.ColumnIndex);
            IRenderer renderer = this.GetCellRenderer(rowObject, column);

            // Get a graphics context for the renderer to use.
            // But we have more complications. Virtual lists have a nasty habit of drawing column 0
            // whenever there is any mouse move events over a row, and doing it in an un-double-buffered manner,
            // which results in nasty flickers! There are also some unbuffered draw when a mouse is first
            // hovered over column 0 of a normal row. So, to avoid all complications,
            // we always manually double-buffer the drawing.
            // Except with Mono, which doesn't seem to handle double buffering at all :-(
            BufferedGraphics buffer = BufferedGraphicsManager.Current.Allocate(e.Graphics, r);
            Graphics g = buffer.Graphics;

            g.TextRenderingHint = ObjectListViewBase.TextRenderingHint;
            g.SmoothingMode = ObjectListViewBase.SmoothingMode;

            // Finally, give the renderer a chance to draw something
            e.DrawDefault = !renderer.RenderSubItem(e, g, r, rowObject);

            if (!e.DrawDefault)
            {
                buffer.Render();
            }

            buffer.Dispose();
        }

        #endregion

        #region Implementation variables

        private bool hasIdleHandler; // has an Idle handler already been installed?
        private bool hasResizeColumnsHandler; // has an idle handler been installed which will handle column resizing?
        private bool isInWmPaintEvent; // is a WmPaint event currently being handled?
        private bool shouldDoCustomDrawing; // should the list do its custom drawing?
        private bool isMarqueSelecting; // Is a marque selection in progress?
        private int suspendSelectionEventCount; // How many unmatched SuspendSelectionEvents() calls have been made?

        private readonly List<GlassPanelForm> glassPanels = new List<GlassPanelForm>(); // The transparent panel that draws overlays
        private Dictionary<string, bool> visitedUrlMap = new Dictionary<string, bool>(); // Which urls have been visited?

        // TODO
        //private CheckBoxSettings checkBoxSettings = new CheckBoxSettings();

        #endregion
    }
}
