using ArdbSharp;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Data
{
    public class DeviceData
    {
        /// <summary>
        /// Adds a new device to database
        /// throws error if Id is duplicated
        /// </summary>
        /// <param name="device">Device to add</param>
        public async ValueTask AddDevice(Device device)
        {
            // check for duplicate
            if (device.Id.Length == 0 && GetDeviceById(device.Id) != null)
                throw new Exception("The device is already exists or its Id is null");

            var db = await DataContext.Db.GetDatabaseAsync(DatabaseName.Devices);
            await db.Value.StringSetAsync("d:" + device.Id, MessagePackSerializer.Serialize(device));
            await db.Value.ListRightPushAsync("d:p:" + device.PlayerId, device.Id);
        }

        /// <summary>
        /// Get the device by its Id
        /// </summary>
        /// <param name="Id">device id</param>
        /// <returns>returns device if found otherwise returns null</returns>
        public async ValueTask<Device> GetDeviceById(string Id)
        {
            var key = await Database.StringGetAsync(DataContext.Db, DatabaseName.Devices, "d:" + Id);
            if (key == null)
                return null;

            var d = MessagePackSerializer.Deserialize<Device>((byte[])key);
            d.Id = Id;
            return d;
        }
    }
}
