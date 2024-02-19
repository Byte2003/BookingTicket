// Loading DataTable with Ajax
var dataTable;
$(document).ready(function () {
    loadDataTable();
})
function loadDataTable() {
    dataTable = $('#movieTable').DataTable({
        "ajax": {
            "url": "/CinemaManager/Movie/GetAllMovies"
        },
        "columns": [
            { "data": "movieID", "width": "20%" },
            { "data": "movieName", "width": "20%" },
            { "data": "releaseDate", "width": "15%" },
            { "data": "endDate", "width": "15%" },
            {
                "data": "movieID",
                "render": function (data) {
                    return `
                       
                            <a href="/CinemaManager/Movie/Update?movie_id=${data}"
                            class="btn btn-primary mx-2"> <i class="bi bi-pencil-square"></i> Edit</a>
                            <a onClick=Delete('/CinemaManager/Movie/Delete?movie_id=${data}')
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
                            'This movie has been deleted.',
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