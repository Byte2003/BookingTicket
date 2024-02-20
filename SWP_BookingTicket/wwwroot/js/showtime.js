// Loading DataTable with Ajax
var dataTable;

$(document).ready(function () {
    loadDataTable();
})
function loadDataTable() {
    dataTable = $('#showtimeTable').DataTable({
        "ajax": {
            "url": "/CinemaManager/Showtime/GetAllShowtime"
        },
        "columns": [
            { "data": "showtimeID", "width": "20%" },
            { "data": "date", "width": "15%" },
            { "data": "time", "width": "15%" },
            { "data": "minute", "width": "15%" },
            { "data": "movie.movieName", "width": "20%" },
            // I want to get the cinma_name here, 
            { "data": "cinema_name","width": "15%" },
            { "data": "room.roomName", "width": "10%" },

            {
                "data": "roomID",
                "render": function (data) {
                    return `
                       
                            <a href="/CinemaManager/Room/Update?room_id=${data}"
                            class="btn btn-primary mx-2"> <i class="bi bi-pencil-square"></i> Edit</a>
                            <a onClick=Delete('/CinemaManager/Room/Delete?room_id=${data}')
                            class="btn btn-danger mx-2"> <i class="bi bi-trash-fill"></i> Delete</a>
					   
                        `
                },
                "width": "25%"
            }
        ]
    });
}

var selectCinema = document.getElementById("cinemaSelectList");
var selectRoom = document.getElementById("roomSelectList");

selectCinema.addEventListener("change", function () {
    var cinema_id = selectCinema.value;
    console.log("change");
    loadroomlist(cinema_id);
});

function loadroomlist(cinemaid) {
    var url = "/CinemaManager/Showtime/GetRoomList?cinema_id=" + cinemaid;
    console.log(url);

    fetch(url)
        .then(response => {
            if (!response.ok) {
                throw new Error("Network response was not ok");
            }
            return response.json();
        })
        .then(data => {
            var selectelement = selectRoom;
            selectelement.innerHTML = ""; // Clear previous options
            var roomList = data.data; // Access the 'data' property from the JSON response
            roomList.forEach(item => {
                var option = document.createElement("option");
                option.text = item.room.roomName; // Truy cập vào thuộc tính roomName của đối tượng room
                option.value = item.room.roomID; // Truy cập vào thuộc tính roomID của đối tượng room
                selectelement.appendChild(option);
            });
        })
        .catch(error => {
            console.error("Error: ", error);
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
                            'This room has been deleted.',
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