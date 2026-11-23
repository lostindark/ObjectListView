/*
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BrightIdeasSoftware
{
    public abstract partial class ObjectListViewBase {
        #region Save/Restore State

        /// <summary>
        /// Return a byte array that represents the current state of the ObjectListView, such
        /// that the state can be restored by RestoreState()
        /// </summary>
        /// <remarks>
        /// <para>The state of an ObjectListView includes the attributes that the user can modify:
        /// <list type="bullet">
        /// <item><description>current view (i.e. Details, Tile, Large Icon...)</description></item>
        /// <item><description>ordered sort columns and directions</description></item>
        /// <item><description>column order</description></item>
        /// <item><description>column widths</description></item>
        /// <item><description>column visibility</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// It does not include selection or the scroll position. Layouts use the versioned OLVS
        /// format shared by .NET Framework and modern .NET, not BinaryFormatter serialization.
        /// </para>
        /// </remarks>
        /// <returns>A byte array representing the state of the ObjectListView</returns>
        public virtual byte[] SaveState() {
            this.RememberDisplayIndicies();

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream)) {
                writer.Write(0x53564C4F); // "OLVS", little endian.
                writer.Write(2);
                writer.Write(this.AllColumns.Count);
                writer.Write((int)this.View);
                OLVColumn primaryColumn = this.PrimarySortColumn;
                SortOrder primaryOrder = this.PrimarySortOrder;
                int primaryIndex = primaryColumn == null ? -1 : this.AllColumns.IndexOf(primaryColumn);
                writer.Write(primaryIndex);
                writer.Write((int)primaryOrder);
                writer.Write(this.ShowGroups);
                foreach (OLVColumn column in this.AllColumns) {
                    writer.Write(column.Width);
                    writer.Write(column.LastDisplayIndex);
                    writer.Write(column.IsVisible);
                }
                var additionalSorts = new List<KeyValuePair<int, SortOrder>>();
                if (primaryIndex >= 0 && primaryOrder != SortOrder.None) {
                    var seen = new HashSet<int> { primaryIndex };
                    foreach (OLVSortDescription sort in this.sortDescriptions.GetActiveDescriptions().Skip(1)) {
                        int index = this.AllColumns.IndexOf(sort.Column);
                        if (index >= 0 && seen.Add(index))
                            additionalSorts.Add(new KeyValuePair<int, SortOrder>(index, sort.SortOrder));
                    }
                }
                writer.Write(additionalSorts.Count);
                foreach (KeyValuePair<int, SortOrder> sort in additionalSorts) {
                    writer.Write(sort.Key);
                    writer.Write((int)sort.Value);
                }
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Restore the state of the control from the given byte array, which must have been
        /// produced by SaveState()
        /// </summary>
        /// <param name="state">A byte array returned from SaveState()</param>
        /// <returns>Returns true if the state was restored. Invalid, incompatible, and legacy
        /// BinaryFormatter layouts return false without changing the control.</returns>
        public virtual bool RestoreState(byte[] state) {
            ArgumentNullException.ThrowIfNull(state, nameof(state));

            // Validate length before allocating or reading. Each column has two Int32s and one byte.
            long originalLength = 25L + 9L * this.AllColumns.Count;
            if (state.LongLength < originalLength)
            {
                return false;
            }

            using (BinaryReader reader = new BinaryReader(new MemoryStream(state, false))) {
                if (reader.ReadInt32() != 0x53564C4F)
                    return false;
                int version = reader.ReadInt32();
                if (reader.ReadInt32() != this.AllColumns.Count ||
                    (version != 1 && version != 2) ||
                    (version == 1 && state.LongLength != originalLength) ||
                    (version == 2 && state.LongLength < originalLength + sizeof(int)))
                {
                    return false;
                }

                int view = reader.ReadInt32();
                int sortColumn = reader.ReadInt32();
                int sortOrder = reader.ReadInt32();
                byte showGroups = reader.ReadByte();
                if (view < (int)View.LargeIcon || view > (int)View.Tile ||
                    sortColumn < -1 || sortColumn >= this.AllColumns.Count ||
                    sortOrder < (int)SortOrder.None || sortOrder > (int)SortOrder.Descending ||
                    showGroups > 1)
                {
                    return false;
                }

                int[] widths = new int[this.AllColumns.Count];
                int[] displayIndices = new int[this.AllColumns.Count];
                bool[] visible = new bool[this.AllColumns.Count];
                for (int i = 0; i < this.AllColumns.Count; i++) {
                    widths[i] = reader.ReadInt32();
                    displayIndices[i] = reader.ReadInt32();
                    byte isVisible = reader.ReadByte();
                    // Hidden columns can retain ordering keys beyond the current count after removals.
                    if (widths[i] < -2 || displayIndices[i] < -1 || isVisible > 1)
                    {
                        return false;
                    }

                    visible[i] = isVisible == 1;
                }

                var additionalSorts = new List<OLVSortDescription>();
                if (version == 2) {
                    int count = reader.ReadInt32();
                    int maxAdditionalSorts = sortColumn >= 0 && sortOrder != (int)SortOrder.None
                        ? this.AllColumns.Count - 1 : 0;
                    if (count < 0 || count > maxAdditionalSorts ||
                        state.LongLength != originalLength + sizeof(int) + 2L * sizeof(int) * count)
                        return false;

                    var seen = new HashSet<int> { sortColumn };
                    for (int i = 0; i < count; i++) {
                        int index = reader.ReadInt32();
                        SortOrder order = (SortOrder)reader.ReadInt32();
                        if (index < 0 || index >= this.AllColumns.Count || !seen.Add(index) ||
                            (order != SortOrder.Ascending && order != SortOrder.Descending))
                            return false;
                        additionalSorts.Add(new OLVSortDescription(this.AllColumns[index], order));
                    }
                }

                // Establish DPI-scaled limits before restoring device-pixel widths.
                if (!this.IsHandleCreated)
                {
                    this.CreateHandle();
                }

                this.sortDescriptions.Clear();
                this.PrimarySortColumn = sortColumn == -1 ? null : this.AllColumns[sortColumn];
                this.PrimarySortOrder = sortColumn == -1 ? SortOrder.None : (SortOrder)sortOrder;
                foreach (OLVSortDescription sort in additionalSorts)
                    this.sortDescriptions.Add(sort);
                for (int i = 0; i < this.AllColumns.Count; i++) {
                    OLVColumn column = this.AllColumns[i];
                    column.Width = widths[i];
                    column.IsVisible = visible[i];
                    column.LastDisplayIndex = displayIndices[i];
                }
                if ((showGroups == 1) != this.ShowGroups)
                {
                    this.ShowGroups = showGroups == 1;
                }

                if (this.View == (View)view)
                {
                    this.RebuildColumns();
                }
                else
                {
                    this.View = (View)view;
                }
            }

            return true;
        }

        #endregion
    }
}
