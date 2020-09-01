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
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Leave_Management.Controllers
{
    [Authorize]
    public class LeaveRequestController : Controller
    {
        private readonly ILeaveRequestRepository _leaveRequestRepo;
        private readonly ILeaveTypeRepository _leaveTypeRepo;
        private readonly ILeaveAllocationRepository _leaveAllocRepo;
        private readonly IMapper _mapper;
        private readonly UserManager<Employee> _userManager;

        public LeaveRequestController(        
            ILeaveRequestRepository leaverequestrepo,
            ILeaveTypeRepository leavetyperepo,
            ILeaveAllocationRepository leaveallocrepo,
            IMapper mapper,
            UserManager<Employee> userManager)
        {
            _leaveRequestRepo = leaverequestrepo;
            _leaveTypeRepo = leavetyperepo;
            _leaveAllocRepo = leaveallocrepo;
            _mapper = mapper;
            _userManager = userManager;
        }

        [Authorize(Roles = "Administrator")]
        // GET: LeaveRequest
        public ActionResult Index()
        {
            var leaveRequests = _leaveRequestRepo.FindAll();
            var leaveRequestsModel = _mapper.Map<List<LeaveRequestVM>>(leaveRequests);
            var model = new AdminLeaveRequestViewVM
            {
                TotalRequests = leaveRequestsModel.Count,
                ApprovedRequests = leaveRequestsModel.Count(q => q.Approved == true),
                PendingRequests = leaveRequestsModel.Count(q => q.Approved == null),
                RejectedRequests = leaveRequestsModel.Count(q => q.Approved == false),
                LeaveRequests = leaveRequestsModel
            };

            return View(model);
        }

        // GET: LeaveRequest/Details/5
        public ActionResult Details(int id)
        {
            var leaveRequest = _leaveRequestRepo.FindById(id);
            var model = _mapper.Map<LeaveRequestVM>(leaveRequest);
            return View(model);
        }

        public ActionResult ApproveRequest (int id)
        {
            try
            {
                var user = _userManager.GetUserAsync(User).Result;
                var leaveRequest = _leaveRequestRepo.FindById(id);
                var employeeid = leaveRequest.RequestingEmployeeId;
                var leaveTypeid = leaveRequest.LeaveTypeId;
                var allocation = _leaveAllocRepo.GetLeaveAllocationsByEmployeeAndType(employeeid, leaveTypeid);
                
                var daysRequested = (int)(leaveRequest.EndDate - leaveRequest.StartDate).TotalDays;
                allocation.NumberOfDays = allocation.NumberOfDays - daysRequested;


                leaveRequest.Approved = true;
                leaveRequest.ApprovedById = user.Id;
                leaveRequest.DateActioned = DateTime.Now;

                _leaveRequestRepo.Update(leaveRequest);
                _leaveAllocRepo.Update(allocation);                    

                return RedirectToAction(nameof(Index));
                
            }
            catch (Exception)
            {
                return RedirectToAction(nameof(Index));
            }
           
        }

        public ActionResult RejectRequest(int id)
        {
            try
            {
                var user = _userManager.GetUserAsync(User).Result;
                var leaveRequest = _leaveRequestRepo.FindById(id);
                leaveRequest.Approved = false;
                leaveRequest.ApprovedById = user.Id;
                leaveRequest.DateActioned = DateTime.Now;

                _leaveRequestRepo.Update(leaveRequest);

                return RedirectToAction(nameof(Index));

            }
            catch (Exception)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        public ActionResult CancelRequest (int id)
        {
            var leaveRequest = _leaveRequestRepo.FindById(id);
            leaveRequest.Cancelled = true;
            _leaveRequestRepo.Update(leaveRequest);

            return RedirectToAction("MyLeave");
        }
        // GET: LeaveRequest/Create
        public ActionResult Create()
        {
            var leavetypes = _leaveTypeRepo.FindAll();
            var leaveTypeItems = leavetypes.Select(q => new SelectListItem
            {
                Text = q.Name,
                Value = q.Id.ToString()
            });

            var model = new CreateLeaveRequestVM
            {
                LeaveTypes = leaveTypeItems
            };
            return View(model);
        }

        // POST: LeaveRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateLeaveRequestVM model)
        {
            try
            {
                var startDate = Convert.ToDateTime(model.StartDate);
                var endDate = Convert.ToDateTime(model.EndDate);
                var leavetypes = _leaveTypeRepo.FindAll();
                var leaveTypeItems = leavetypes.Select(q => new SelectListItem
                {
                    Text = q.Name,
                    Value = q.Id.ToString()
                });

                model.LeaveTypes = leaveTypeItems;


                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                if (DateTime.Compare(startDate, endDate) > 1)
                {
                    //The start date is after the end date
                    ModelState.AddModelError("", "Start Date cannot be further in the future than the End Date");
                    return View(model);
                }

                var employee = _userManager.GetUserAsync(User).Result;
                var allocation = _leaveAllocRepo.GetLeaveAllocationsByEmployeeAndType(employee.Id, model.LeaveTypeId);
                var daysRequested = (int)(endDate - startDate).TotalDays;

                if (daysRequested > allocation.NumberOfDays)
                {
                    ModelState.AddModelError("", "You do not have sufficient days for this request.");
                    return View(model);
                }

                var leaveRequestModel = new LeaveRequestVM
                {
                    LeaveTypeId = model.LeaveTypeId,
                    Approved = null,
                    StartDate = startDate,
                    EndDate = endDate,
                    DateRequested = DateTime.Now,
                    DateActioned = DateTime.Now,
                    RequestingEmployeeId = employee.Id,
                    RequestComments = model.RequestComments

                };

                var leaveRequest = _mapper.Map<LeaveRequest>(leaveRequestModel);
                var isSuccess = _leaveRequestRepo.Create(leaveRequest);

                if (!isSuccess)
                {
                    ModelState.AddModelError("", "Something went wrong with submitting your record.");
                    return View(model);
                }

                //To be changed later....
                return RedirectToAction(nameof(Index), "Home");
            }
            catch
            {
                return View(model);
            }
        }

        public ActionResult MyLeave()
        {
            var employee = _userManager.GetUserAsync(User).Result;
            var employeeid = employee.Id;
            var employeeAllocations = _leaveAllocRepo.GetLeaveAllocationsByEmployee(employeeid);
            var employeeRequests = _leaveRequestRepo.GetLeaveRequestByEmployee(employeeid);

            var employeeAllocationsModel = _mapper.Map<List<LeaveAllocationVM>>(employeeAllocations);
            var employeeRequestsModel = _mapper.Map<List<LeaveRequestVM>>(employeeRequests);

            var model = new EmployeeLeaveRequestViewVM
            {
                LeaveAllocations = employeeAllocationsModel,
                LeaveRequests = employeeRequestsModel
            };

            return View(model);
            
        }

        // GET: LeaveRequest/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: LeaveRequest/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Something went wrong...");
                return View();
            }
        }

        // GET: LeaveRequest/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: LeaveRequest/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
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