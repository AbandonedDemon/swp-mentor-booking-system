// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function filterUser() {
    // Declare variables
    var input, filter, table, tr, td, i, txtValue;
    input = document.getElementById("searchInput");
    filter = input.value.toUpperCase();
    table = document.getElementById("userTable");
    tr = table.getElementsByTagName("tr");

    // Loop through all table rows, and hide those who don't match the search query
    for (i = 0; i < tr.length; i++) {
        td = tr[i].getElementsByTagName("td")[0];
        if (td) {
            txtValue = td.textContent || td.innerText;
            if (txtValue.toUpperCase().indexOf(filter) > -1) {
                tr[i].style.display = "";
            } else {
                tr[i].style.display = "none";
            }
        }
    }
}

const loadMoreBtn = document.getElementById('loadMoreBtn');
const hiddenMentors = document.querySelectorAll('.d-none');
let currentIndex = 0;

loadMoreBtn.addEventListener('click', () => {
    for (let i = 0; i < 9 && currentIndex < hiddenMentors.length; i++) {
        hiddenMentors[currentIndex].classList.remove('d-none');
        currentIndex++;
    }
    if (currentIndex >= hiddenMentors.length) {
        loadMoreBtn.style.display = 'none';
    }
});