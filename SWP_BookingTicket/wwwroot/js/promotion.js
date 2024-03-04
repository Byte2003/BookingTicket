//var dataTable;
//$(document).ready(function () {
//    loadDataTable();
//})
//function loadDataTable() {
//    dataTable = $('#promotionTable').DataTable({
//        "ajax": {
//            "url": "/CinemaManager/Promotion/GetAllPromotions"
//        },
//        "columns": [
//            { "data": "promotionID", "width": "15%" },
//            { "data": "topic", "width": "15%" },
//            { "data": "content", "width": "25%" },
//            {
//                "data": "startDate",
//                "width": "15%",
//                "render": function (data) {
//                    return moment(data).format('YYYY-MM-DD HH:mm:ss'); // Định dạng ngày giờ
//                }
//            },
//            {
//                "data": "endDate",
//                "width": "15%",
//                "render": function (data) {
//                    return moment(data).format('YYYY-MM-DD HH:mm:ss'); // Định dạng ngày giờ
//                }
//            }, "data": "endDate", "width": "15%" },
//            {
//                "data": "imageUrl",
//                "render": function (data) {
//                    return `<td class="image-column" style="height:150px">
//                                <img src="${data}" alt="Alternate Text" class=" img-fluid image-element" />
//                            </td>`;
//                },
//                "width": "15%"
//            },

//            {
//                "data": "promotionID",
//                "render": function (data) {
//                    return `
                       
//                            <a href="/CinemaManager/Promotion/Update?promotion_id=${data}"
//                            class="btn btn-primary mx-2"> <i class="bi bi-pencil-square"></i> Edit</a>
//                            <a onClick=Delete('/CinemaManager/Promotion/Delete?promotion_id=${data}')
//                            class="btn btn-danger mx-2"> <i class="bi bi-trash-fill"></i> Delete</a>
					   
//                        `
//                },
//                "width": "25%"
//            }
//        ]
//    });
//}

//// SweetAlert library
//function Delete(url) {
//    Swal.fire({
//        title: 'Are you sure?',
//        text: "You won't be able to revert this!",
//        icon: 'warning',
//        showCancelButton: true,
//        confirmButtonColor: '#3085d6',
//        cancelButtonColor: '#d33',
//        confirmButtonText: 'Yes, delete it!'
//    }).then((result) => {
//        if (result.isConfirmed) {
//            $.ajax({
//                url: url,
//                type: 'DELETE',
//                success: function (data) {
//                    if (data.success) {
//                        dataTable.ajax.reload();
//                        Swal.fire(
//                            'Deleted!',
//                            'This promotion has been deleted.',
//                            'success'
//                        )
//                        //toastr.success(data.message);
//                    } else {
//                        //toastr.error(data.message);
//                    }
//                }
//            })
//        }
//    })
//}