$(function () {
    $("#loginForm").on("submit", function (e) {
        e.preventDefault();

        const email = $("#floatingInput").val().trim();
        const password = $("#floatingPassword").val();

        $("#loginError").addClass("d-none").text("");

        if (!email || !password) {
            $("#loginError").removeClass("d-none").text("Vui lòng nhập Email và Password.");
            return;
        }

        // Disable button + show loading
        const $btn = $("#btnSignIn");
        const oldText = $btn.text();
        $btn.prop("disabled", true).text("Signing in...");

        // Lấy anti-forgery token (từ @Html.AntiForgeryToken())
        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: "/admin/Login", // <-- đổi route đúng controller/action của bạn
            type: "POST",
            contentType: "application/x-www-form-urlencoded; charset=UTF-8",
            data: {
                __RequestVerificationToken: token,
                email: email,
                password: password,
                rememberMe: $("#exampleCheck1").is(":checked")
            },
            success: function (res) {
                if (res && res.success) {
                    window.location.href = res.redirectUrl;
                } else {
                    $("#loginError").removeClass("d-none").text(res.message || "Đăng nhập thất bại.");
                }
            },
            error: function (xhr) {
                let msg = "Đăng nhập thất bại. Vui lòng thử lại.";
                try {
                    const json = xhr.responseJSON;
                    if (json && json.message) msg = json.message;
                } catch (e) { }
                $("#loginError").removeClass("d-none").text(msg);
            },
            complete: function () {
                $btn.prop("disabled", false).text(oldText);
            }
        });
    });
});
