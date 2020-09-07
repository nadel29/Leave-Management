using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Leave_Management.Contracts;
using Leave_Management.Data;
using Leave_Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Leave_Management.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class LeaveAllocationController : Controller
    {
        private readonly ILeaveTypeRepository _leaverepo;
        private readonly ILeaveAllocationRepository _leaveallocationrepo;
        private readonly IMapper _mapper;
        private readonly UserManager<Employee> _userManager;

        public LeaveAllocationController(ILeaveTypeRepository leaverepo, 
            ILeaveAllocationRepository leaveallocationrepo,
            IMapper mapper,
            UserManager<Employee> userManager)
        {
            _leaverepo = leaverepo;
            _leaveallocationrepo = leaveallocationrepo;
            _mapper = mapper;
            _userManager = userManager;
        }
        // GET: LeaveAllocation
        public async Task<ActionResult> Index()
        {
            var leavetypes = await _leaverepo.FindAll();
            var mappedLeaveTypes = _mapper.Map<List<LeaveType>, List<LeaveTypeVM>>(leavetypes.ToList());
            var model = new CreateLeaveAllocationVM
            {
                LeaveTypes = mappedLeaveTypes,
                NumberUpdated = 0 //challenge
            };
            return View(model);
        }

        public async Task<ActionResult> SetLeave (int id)
        {
            var leavetype = await _leaverepo.FindById(id);
            var employees = await _userManager.GetUsersInRoleAsync("Employee");

            foreach (var emp in employees)
            {
                if (await _leaveallocationrepo.CheckAllocation(id, emp.Id))
                    continue; //continue looping
                var allocation = new LeaveAllocationVM
                {
                    EmployeeId = emp.Id,
                    LeaveTypeId = id,
                    Period = DateTime.Now.Year,
                    NumberOfDays = leavetype.DefaultDays,
                    DateCreated = DateTime.Now
                };
                var leaveallocation = _mapper.Map<LeaveAllocation>(allocation);
                await _leaveallocationrepo.Create(leaveallocation);

            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<ActionResult> ListEmployees()
        {
            var employees = await _userManager.GetUsersInRoleAsync("Employee");
            var model = _mapper.Map<List<EmployeeVM>>(employees);
            return View(model);
        }

        // GET: LeaveAllocation/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var employee = _mapper.Map<EmployeeVM>(await _userManager.FindByIdAsync(id));
            
            var allocations = _mapper.Map<List<LeaveAllocationVM>>(await _leaveallocationrepo.GetLeaveAllocationsByEmployee(id));

            var model = new ViewAllocationsVM
            {               
                Employee = employee,
                LeaveAllocations = allocations

            };
            return View(model);
        }

        // GET: LeaveAllocation/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: LeaveAllocation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(IFormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: LeaveAllocation/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            var leaveAllocation = await _leaveallocationrepo.FindById(id);
            var model = _mapper.Map<EditLeaveAllocationVM>(leaveAllocation);
            return View(model);
        }

        // POST: LeaveAllocation/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(EditLeaveAllocationVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var record = await _leaveallocationrepo.FindById(model.Id);
                record.NumberOfDays = model.NumberOfDays;
               
                var isSucceed = await _leaveallocationrepo.Update(record);
                if (!isSucceed)
                {
                    ModelState.AddModelError("", "Something went wrong...");
                    return View(model);
                }

                return RedirectToAction(nameof(Details), new { id = model.EmployeeId});
            }
            catch
            {
                ModelState.AddModelError("", "Something went wrong...");
                return View();
            }
        }

        // GET: LeaveAllocation/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: LeaveAllocation/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}