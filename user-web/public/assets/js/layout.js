$(document).ready(function () {

    $(".DemoSamples").click(function () {
        $('html, body').animate({
            scrollTop: $(".demo-samples").offset().top
        }, 3000); 
    });
    
    $(".OurPlans").click(function () {
        $('html, body').animate({
            scrollTop: $(".pricing").offset().top
        }, 3000); 
    });
});