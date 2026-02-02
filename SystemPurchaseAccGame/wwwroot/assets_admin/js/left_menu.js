$(document).ready(function () {
    // map menu -> action
    const routes = {
        item_bar_account: "/Admin/AccGame",
        item_bar_upload_acc: "/Admin/UploadAccGame",
        item_bar_sold: "/Admin/SoldAccGame" // nếu chưa có action này thì bạn tạo sau
    };

    function setActiveMenu(clickedId) {
        // bỏ active tất cả dropdown-item trong cùng dropdown
        $("#" + clickedId)
            .closest(".dropdown-menu")
            .find(".dropdown-item")
            .removeClass("active");

        // active item đang chọn
        $("#" + clickedId).addClass("active");
    }

    function loadPartial(url, clickedId) {
        // show loading
        $("#content_main_layout").html(`
        <div class="bg-secondary rounded p-4">
          <div class="d-flex align-items-center">
            <div class="spinner-border text-primary me-3" role="status"></div>
            <div>Đang tải dữ liệu...</div>
          </div>
        </div>
      `);

        $.ajax({
            url: url,
            method: "GET",
            cache: false,
            success: function (html) {
                $("#content_main_layout").html(html);
                if (clickedId) setActiveMenu(clickedId);
            },
            error: function (xhr) {
                $("#content_main_layout").html(`
            <div class="alert alert-danger">
              Không tải được nội dung. Status: ${xhr.status}
            </div>
          `);
            }
        });
    }

    // bind click events
    $("#item_bar_account").on("click", function (e) {
        e.preventDefault();
        loadPartial(routes.item_bar_account, "item_bar_account");
    });

    $("#item_bar_upload_acc").on("click", function (e) {
        e.preventDefault();
        loadPartial(routes.item_bar_upload_acc, "item_bar_upload_acc");
    });

    $("#item_bar_sold").on("click", function (e) {
        e.preventDefault();
        // Nếu bạn chưa có action SoldAccGame thì comment dòng này lại
        loadPartial(routes.item_bar_sold, "item_bar_sold");
    });

    // Option: load mặc định khi vào Dashboard (ví dụ mở "Tài khoản")
    loadPartial(routes.item_bar_account, "item_bar_account");
});
