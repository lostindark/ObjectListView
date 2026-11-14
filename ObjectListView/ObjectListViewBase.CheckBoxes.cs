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
    public abstract partial class ObjectListViewBase {
        #region Checkboxes

        /// <summary>
        /// Check all rows
        /// </summary>
        /// <remarks>This checks every model, including models hidden by a filter.</remarks>
        public virtual void CheckAll()
        {
            this.CheckedModelObjects = EnumerableToArray(this.ModelObjects, false);
        }

        /// <summary>
        /// Check the checkbox in the given column header
        /// </summary>
        /// <remarks>If the given columns header check box is linked to the cell check boxes,
        /// then checkboxes in all cells will also be checked.</remarks>
        /// <param name="column"></param>
        public virtual void CheckHeaderCheckBox(OLVColumn column)
        {
            if (column == null)
            {
                return;
            }

            ChangeHeaderCheckBoxState(column, CheckState.Checked);
        }

        /// <summary>
        /// Mark the checkbox in the given column header as having an indeterminate value
        /// </summary>
        /// <param name="column"></param>
        public virtual void CheckIndeterminateHeaderCheckBox(OLVColumn column)
        {
            if (column == null)
            {
                return;
            }

            ChangeHeaderCheckBoxState(column, CheckState.Indeterminate);
        }

        /// <summary>
        /// Mark the given object as indeterminate check state
        /// </summary>
        /// <param name="modelObject">The model object to be marked indeterminate</param>
        public virtual void CheckIndeterminateObject(object modelObject) {
            this.SetObjectCheckedness(modelObject, CheckState.Indeterminate);
        }

        /// <summary>
        /// Mark the given object as checked in the list
        /// </summary>
        /// <param name="modelObject">The model object to be checked</param>
        public virtual void CheckObject(object modelObject) {
            this.SetObjectCheckedness(modelObject, CheckState.Checked);
        }

        /// <summary>
        /// Mark the given objects as checked in the list
        /// </summary>
        /// <param name="modelObjects">The model object to be checked</param>
        public virtual void CheckObjects(IEnumerable modelObjects) {
            foreach (object model in modelObjects)
            {
                this.CheckObject(model);
            }
        }

        /// <summary>
        /// Put a check into the check box at the given cell
        /// </summary>
        /// <param name="rowObject"></param>
        /// <param name="column"></param>
        public virtual void CheckSubItem(object rowObject, OLVColumn column) {
            if (column == null || rowObject == null || !column.CheckBoxes)
            {
                return;
            }

            column.PutCheckState(rowObject, CheckState.Checked);
            this.RefreshObject(rowObject);
        }

        /// <summary>
        /// Put an indeterminate check into the check box at the given cell
        /// </summary>
        /// <param name="rowObject"></param>
        /// <param name="column"></param>
        public virtual void CheckIndeterminateSubItem(object rowObject, OLVColumn column) {
            if (column == null || rowObject == null || !column.CheckBoxes)
            {
                return;
            }

            column.PutCheckState(rowObject, CheckState.Indeterminate);
            this.RefreshObject(rowObject);
        }

        /// <summary>
        /// Return true of the given object is checked
        /// </summary>
        /// <param name="modelObject">The model object whose checkedness is returned</param>
        /// <returns>Is the given object checked?</returns>
        /// <remarks>If the given object is not in the list, this method returns false.</remarks>
        public virtual bool IsChecked(object modelObject) {
            return this.GetCheckState(modelObject) == CheckState.Checked;
        }

        /// <summary>
        /// Return true of the given object is indeterminately checked
        /// </summary>
        /// <param name="modelObject">The model object whose checkedness is returned</param>
        /// <returns>Is the given object indeterminately checked?</returns>
        /// <remarks>If the given object is not in the list, this method returns false.</remarks>
        public virtual bool IsCheckedIndeterminate(object modelObject) {
            return this.GetCheckState(modelObject) == CheckState.Indeterminate;
        }

        /// <summary>
        /// Is there a check at the check box at the given cell
        /// </summary>
        /// <param name="rowObject"></param>
        /// <param name="column"></param>
        public virtual bool IsSubItemChecked(object rowObject, OLVColumn column) {
            if (column == null || rowObject == null || !column.CheckBoxes)
            {
                return false;
            }

            return (column.GetCheckState(rowObject) == CheckState.Checked);
        }

        /// <summary>
        /// Get the checkedness of an object from the model. Returning null means the
        /// model does not know and the value from the control will be used.
        /// </summary>
        /// <param name="modelObject"></param>
        /// <returns></returns>
        protected virtual CheckState? GetCheckState(Object modelObject) {
            if (this.CheckStateGetter != null)
            {
                return this.CheckStateGetter(modelObject);
            }

            return this.PersistentCheckBoxes ? this.GetPersistentCheckState(modelObject) : (CheckState?)null;
        }

        /// <summary>
        /// Record the change of checkstate for the given object in the model.
        /// This does not update the UI -- only the model
        /// </summary>
        /// <param name="modelObject"></param>
        /// <param name="state"></param>
        /// <returns>The check state that was recorded and that should be used to update
        /// the control.</returns>
        protected virtual CheckState PutCheckState(Object modelObject, CheckState state) {
            if (this.CheckStatePutter != null)
            {
                return this.CheckStatePutter(modelObject, state);
            }

            return this.PersistentCheckBoxes ? this.SetPersistentCheckState(modelObject, state) : state;
        }

        /// <summary>
        /// Change the check state of the given object to be the given state.
        /// </summary>
        /// <remarks>
        /// If the given model object isn't in the list, we still try to remember
        /// its state, in case it is referenced in the future.</remarks>
        /// <param name="modelObject"></param>
        /// <param name="state"></param>
        /// <returns>True if the checkedness of the model changed</returns>
        protected virtual bool SetObjectCheckedness(object modelObject, CheckState state) {

            if (GetCheckState(modelObject) == state)
            {
                return false;
            }

            OLVListItem olvi = this.ModelToItem(modelObject);

            // If we didn't find the given, we still try to record the check state.
            if (olvi == null) {
                this.PutCheckState(modelObject, state);
                return true;
            }

            // Trigger checkbox changing event
            ItemCheckEventArgs ice = new ItemCheckEventArgs(olvi.Index, state, olvi.CheckState);
            this.OnItemCheck(ice);
            if (ice.NewValue == olvi.CheckState)
            {
                return false;
            }

            olvi.CheckState = this.PutCheckState(modelObject, state);
            this.RefreshItem(olvi);

            // Trigger check changed event
            this.OnItemChecked(new ItemCheckedEventArgs(olvi));
            return true;
        }

        /// <summary>
        /// Toggle the checkedness of the given object. A checked object becomes
        /// unchecked; an unchecked or indeterminate object becomes checked.
        /// If the list has tristate checkboxes, the order is:
        ///    unchecked -> checked -> indeterminate -> unchecked ...
        /// </summary>
        /// <param name="modelObject">The model object to be checked</param>
        public virtual void ToggleCheckObject(object modelObject) {
            OLVListItem olvi = this.ModelToItem(modelObject);
            if (olvi == null)
            {
                return;
            }

            CheckState newState = CheckState.Checked;

            if (olvi.CheckState == CheckState.Checked) {
                newState = this.TriStateCheckBoxes ? CheckState.Indeterminate : CheckState.Unchecked;
            } else {
                if (olvi.CheckState == CheckState.Indeterminate && this.TriStateCheckBoxes)
                {
                    newState = CheckState.Unchecked;
                }
            }
            this.SetObjectCheckedness(modelObject, newState);
        }

        /// <summary>
        /// Toggle the checkbox in the header of the given column
        /// </summary>
        /// <remarks>Obviously, this is only useful if the column actually has a header checkbox.</remarks>
        /// <param name="column"></param>
        public virtual void ToggleHeaderCheckBox(OLVColumn column) {
            if (column == null)
            {
                return;
            }

            CheckState newState = CalculateToggledCheckState(column.HeaderCheckState, column.HeaderTriStateCheckBox, column.HeaderCheckBoxDisabled);
            ChangeHeaderCheckBoxState(column, newState);
        }

        private void ChangeHeaderCheckBoxState(OLVColumn column, CheckState newState) {
            // Tell the world the checkbox was clicked
            HeaderCheckBoxChangingEventArgs args = new HeaderCheckBoxChangingEventArgs();
            args.Column = column;
            args.NewCheckState = newState;

            this.OnHeaderCheckBoxChanging(args);
            if (args.Cancel || column.HeaderCheckState == args.NewCheckState)
            {
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();

            column.HeaderCheckState = args.NewCheckState;
            this.HeaderControl.Invalidate(column);

            if (column.HeaderCheckBoxUpdatesRowCheckBoxes) {
                if (column.Index == 0)
                {
                    this.UpdateAllPrimaryCheckBoxes(column);
                }
                else
                {
                    this.UpdateAllSubItemCheckBoxes(column);
                }
            }

            // Debug.WriteLine(String.Format("PERF - Changing row checkboxes on {2} objects took {0}ms / {1} ticks", sw.ElapsedMilliseconds, sw.ElapsedTicks, this.GetItemCount()));
        }

        private void UpdateAllPrimaryCheckBoxes(OLVColumn column) {
            if (!this.CheckBoxes || column.HeaderCheckState == CheckState.Indeterminate)
            {
                return;
            }

            if (column.HeaderCheckState == CheckState.Checked)
            {
                CheckAll();
            }
            else
            {
                UncheckAll();
            }
        }

        private void UpdateAllSubItemCheckBoxes(OLVColumn column) {
            if (!column.CheckBoxes || column.HeaderCheckState == CheckState.Indeterminate)
            {
                return;
            }

            IEnumerable source = this.ModelObjects;
            if (source != null) {
                foreach (object model in source)
                {
                    column.PutCheckState(model, column.HeaderCheckState);
                }
            }
            this.BuildList(true);
        }

        /// <summary>
        /// Toggle the check at the check box of the given cell
        /// </summary>
        /// <param name="rowObject"></param>
        /// <param name="column"></param>
        public virtual void ToggleSubItemCheckBox(object rowObject, OLVColumn column) {
            CheckState currentState = column.GetCheckState(rowObject);
            CheckState newState = CalculateToggledCheckState(currentState, column.TriStateCheckBoxes, false);

            SubItemCheckingEventArgs args = new SubItemCheckingEventArgs(column, this.ModelToItem(rowObject), column.Index, currentState, newState);
            this.OnSubItemChecking(args);
            if (args.Canceled)
            {
                return;
            }

            switch (args.NewValue) {
                case CheckState.Checked:
                    this.CheckSubItem(rowObject, column);
                    break;
                case CheckState.Indeterminate:
                    this.CheckIndeterminateSubItem(rowObject, column);
                    break;
                case CheckState.Unchecked:
                    this.UncheckSubItem(rowObject, column);
                    break;
            }
        }

        /// <summary>
        /// Uncheck all rows
        /// </summary>
        /// <remarks>This unchecks every model, including models hidden by a filter.</remarks>
        public virtual void UncheckAll()
        {
            this.CheckedModelObjects = null;
        }

        /// <summary>
        /// Mark the given object as unchecked in the list
        /// </summary>
        /// <param name="modelObject">The model object to be unchecked</param>
        public virtual void UncheckObject(object modelObject) {
            this.SetObjectCheckedness(modelObject, CheckState.Unchecked);
        }

        /// <summary>
        /// Mark the given objects as unchecked in the list
        /// </summary>
        /// <param name="modelObjects">The model object to be checked</param>
        public virtual void UncheckObjects(IEnumerable modelObjects) {
            foreach (object model in modelObjects)
            {
                this.UncheckObject(model);
            }
        }

        /// <summary>
        /// Uncheck the checkbox in the given column header
        /// </summary>
        /// <param name="column"></param>
        public virtual void UncheckHeaderCheckBox(OLVColumn column)
        {
            if (column == null)
            {
                return;
            }

            ChangeHeaderCheckBoxState(column, CheckState.Unchecked);
        }

        /// <summary>
        /// Uncheck the check at the given cell
        /// </summary>
        /// <param name="rowObject"></param>
        /// <param name="column"></param>
        public virtual void UncheckSubItem(object rowObject, OLVColumn column)
        {
            if (column == null || rowObject == null || !column.CheckBoxes)
            {
                return;
            }

            column.PutCheckState(rowObject, CheckState.Unchecked);
            this.RefreshObject(rowObject);
        }

        #endregion

        #region Persistent check state

        /// <summary>
        /// Gets the checkedness of the given model.
        /// </summary>
        /// <param name="model">The model</param>
        /// <returns>The checkedness of the model. Defaults to unchecked.</returns>
        protected virtual CheckState GetPersistentCheckState(object model) {
            CheckState state;
            if (model != null && this.CheckStateMap.TryGetValue(model, out state))
            {
                return state;
            }

            return CheckState.Unchecked;
        }

        /// <summary>
        /// Remember the check state of the given model object
        /// </summary>
        /// <param name="model">The model to be remembered</param>
        /// <param name="state">The model's checkedness</param>
        /// <returns>The state given to the method</returns>
        protected virtual CheckState SetPersistentCheckState(object model, CheckState state) {
            if (model == null)
            {
                return CheckState.Unchecked;
            }

            this.CheckStateMap[model] = state;
            return state;
        }

        /// <summary>
        /// Forget any persistent checkbox state
        /// </summary>
        protected virtual void ClearPersistentCheckState() {
            this.CheckStateMap = null;
        }

        #endregion
    }
}
