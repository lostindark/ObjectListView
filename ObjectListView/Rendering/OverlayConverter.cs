/*
 * Copyright (C) 2009-2014 Phillip Piper
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;

namespace BrightIdeasSoftware.Design
{
    /// <summary>
    /// Control how the overlay is presented in property grids.
    /// </summary>
    internal sealed class OverlayConverter : ExpandableObjectConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
            if (destinationType == typeof(string)) {
                ImageOverlay imageOverlay = value as ImageOverlay;
                if (imageOverlay != null) {
                    return imageOverlay.Image == null ? "(none)" : "(set)";
                }
                TextOverlay textOverlay = value as TextOverlay;
                if (textOverlay != null) {
                    return String.IsNullOrEmpty(textOverlay.Text) ? "(none)" : "(set)";
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
