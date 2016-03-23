/* license-start
 * 
 * Copyright (C) 2008 - 2015 Crispico Resonate, <http://www.crispico.com/>.
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation version 3.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details, at <http://www.gnu.org/licenses/>.
 * 
 * license-end
 */

namespace IotUiApp
{
    /// <summary>
    /// Class that has the structure of a received message. It is used in the process of deserializing the message.
    /// </summary>
    class ReceivedData
    {
        public int temperature { get; set; }
        public int presetTemperature { get; set; }
        public int heater { get; set; }
        public int humidity { get; set; }
    }
}
