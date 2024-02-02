// Loading DataTable with Ajax
var dataTable;
var selectElement = document.getElementById("roomSelected");
var addSeatBtn = document.getElementById('addSeatBtn');

selectElement.addEventListener("change", function () {
    var selectedValue = selectElement.value;
    loadDataTable(selectedValue);
});

$(document).ready(function () {
    loadDataTable("");
})
function loadDataTable(room_id) {
    if (room_id.length != "") {
        addSeatBtn.href = 'Seat/Create?room_id=' + encodeURIComponent(room_id);
        addSeatBtn.removeAttribute('hidden');
    }
    else {
        addSeatBtn.setAttribute('hidden', '');
    }
    var url = "/CinemaManager/Seat/GetSeatList?room_id=" + room_id;

    if (dataTable) {
        dataTable.destroy();
    }
    dataTable = $('#seatTable').DataTable({
        "ajax": {
            "url": url,
        },
        "columns": [
            { "data": "room.roomName", "width": "10%" },
            { "data": "seatID", "width": "25%" },
            { "data": "seatName", "width": "10%" },
            {
                "data": "seatStatus",
                "render": function (data) {
                    var statusText = data ? "Available" : "Locked";
                    return statusText;
                },
                "width": "10%"
            },
            {
                "data": "seatID",
                "render": function (data) {
                    return `

                            <a href="/CinemaManager/Seat/Update?seat_id=${data}"
                            class="btn btn-primary mx-2"> <i class="bi bi-pencil-square"></i> Edit</a>
                            <a onClick=Delete('/CinemaManager/Seat/Delete?seat_id=${data}')
                            class="btn btn-danger mx-2"> <i class="bi bi-trash-fill"></i> Delete</a>

                        `
                },
                "width": "25%"
            }
        ]
    });
}

// SweetAlert library
function Delete(url) {
    Swal.fire({
        title: 'Are you sure?',
        text: "You won't be able to revert this!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'DELETE',
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        Swal.fire(
                            'Deleted!',
                            'This delete has been deleted.',
                            'success'
                        )
                        //toastr.success(data.message);
                    } else {
                        //toastr.error(data.message);
                    }
                }
            })
        }
    })
}