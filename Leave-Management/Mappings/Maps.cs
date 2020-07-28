using AutoMapper;
using Leave_Management.Data;
using Leave_Management.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Leave_Management.Mappings
{
    public class Maps : Profile
    {
        public Maps()
        {
            //They can be mapped in both directions
            CreateMap<LeaveType, DetailsLeaveTypeVM>().ReverseMap();
            CreateMap<LeaveType, CreateLeaveTypeVM>().ReverseMap();

            CreateMap<Employee, EmployeeVM>().ReverseMap();
            CreateMap<LeaveHistory, LeaveHistoryVM>().ReverseMap();
            CreateMap<LeaveAllocation, LeaveAllocationVM>().ReverseMap();
        }
    }
}
