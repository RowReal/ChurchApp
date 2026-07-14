using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class VehicleRecordService
    {
        private readonly AppDbContext _context;
        public VehicleRecordService(AppDbContext context) => _context = context;

        public async Task<List<VehicleRecord>> GetAllVehicleRecordsAsync()
        {
            return await _context.VehicleRecords.AsNoTracking()
                .Include(x => x.Service).Include(x => x.RecordedBy)
                .OrderByDescending(x => x.RecordDate)
                .ThenByDescending(x => x.RecordedAt).ToListAsync();
        }

        public async Task<int> CreateVehicleRecordAsync(VehicleRecord model)
        {
            Validate(model);
            if (await _context.VehicleRecords.AnyAsync(x => x.ServiceId == model.ServiceId && x.RecordDate.Date == model.RecordDate.Date))
                throw new Exception("A vehicle record already exists for this service and date.");
            if (!await _context.Services.AnyAsync(x => x.Id == model.ServiceId))
                throw new Exception("The selected service could not be found.");
            if (!await _context.Workers.AnyAsync(x => x.WorkerId == model.RecordedByWorkerId && x.IsActive))
                throw new Exception("The current worker could not be found or is inactive.");
            model.RecordDate = model.RecordDate.Date;
            model.RecordedAt = DateTime.UtcNow;
            _context.VehicleRecords.Add(model);
            await _context.SaveChangesAsync();
            return model.Id;
        }

        public async Task UpdateVehicleRecordAsync(VehicleRecord model)
        {
            Validate(model);
            var record = await _context.VehicleRecords.FirstOrDefaultAsync(x => x.Id == model.Id)
                ?? throw new Exception("Vehicle record not found.");
            if (await _context.VehicleRecords.AnyAsync(x => x.Id != model.Id && x.ServiceId == model.ServiceId && x.RecordDate.Date == model.RecordDate.Date))
                throw new Exception("Another vehicle record already exists for this service and date.");
            if (!await _context.Services.AnyAsync(x => x.Id == model.ServiceId))
                throw new Exception("The selected service could not be found.");
            record.ServiceId = model.ServiceId;
            record.RecordDate = model.RecordDate.Date;
            record.NumberOfVehicles = model.NumberOfVehicles;
            record.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteVehicleRecordAsync(int recordId)
        {
            var record = await _context.VehicleRecords.FirstOrDefaultAsync(x => x.Id == recordId)
                ?? throw new Exception("Vehicle record not found.");
            _context.VehicleRecords.Remove(record);
            await _context.SaveChangesAsync();
        }

        private static void Validate(VehicleRecord model)
        {
            if (model.ServiceId <= 0) throw new Exception("Please select a service.");
            if (model.RecordDate.Date > DateTime.Today) throw new Exception("Record date cannot be in the future.");
            if (model.NumberOfVehicles < 0) throw new Exception("Number of vehicles cannot be negative.");
        }
    }
}