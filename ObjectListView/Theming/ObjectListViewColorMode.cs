/*
 * Copyright (C) 2011-2014 Phillip Piper
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
using System.Text;

namespace BrightIdeasSoftware
{
    /// <summary>Determines the application-wide default palette for ObjectListView.</summary>
    public enum ObjectListViewColorMode {
        /// <summary>Follow the app's WinForms mode on .NET 10, or the sampled Windows preference on Framework.</summary>
        System,
        /// <summary>Use native Windows light colors.</summary>
        Light,
        /// <summary>Use the dark palette unless high contrast is enabled.</summary>
        Dark
    }
}
