const gulp = require('gulp');
const concat = require('gulp-concat');
const sourcemaps = require('gulp-sourcemaps');
const cleanCss = require('gulp-clean-css');
const uglify = require('gulp-uglify');

const appStyles = [
    'Assets/css/site.css'
];

const vendorStyles = [
    'node_modules/bootstrap/dist/css/bootstrap.css',
    'node_modules/prismjs/themes/prism.css'
];

const appScripts = [
    'Assets/js/site.js'
];

const vendorScripts = [
    'node_modules/jquery/dist/jquery.js',
    'node_modules/bootstrap/dist/js/bootstrap.js',
    'node_modules/prismjs/prism.js',
    'node_modules/prismjs/components/prism-csharp.js'
];

const buildVendorCss = () => {
    return gulp.src(vendorStyles)
        .pipe(sourcemaps.init())
        .pipe(sourcemaps.init())
        .pipe(concat('vendor.min.css'))
        .pipe(cleanCss())
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest('wwwroot/css/'));
};

const buildVendorJs = () => {
    return gulp.src(vendorScripts)
        .pipe(sourcemaps.init())
        .pipe(concat('vendor.min.js'))
        .pipe(uglify())
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest('wwwroot/js/'));
};

const buildCss = () => {
    return gulp.src(appStyles)
        .pipe(sourcemaps.init())
        .pipe(concat('site.min.css'))
        .pipe(cleanCss())
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest('wwwroot/css/'));
};

const buildJs = () => {
    return gulp.src(appScripts)
        .pipe(sourcemaps.init())
        .pipe(concat('site.min.js'))
        .pipe(uglify())
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest('wwwroot/js/'));
};

const buildVendor = gulp.parallel(buildVendorCss, buildVendorJs);
const buildApp = gulp.parallel(buildCss, buildJs);

const defaultTasks = gulp.parallel(buildVendor, buildApp);

exports.buildApp = buildApp;
exports.buildVendor = buildVendor;
exports.default = defaultTasks;
