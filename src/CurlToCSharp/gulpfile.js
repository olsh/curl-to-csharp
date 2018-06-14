const gulp = require('gulp');
const concat = require('gulp-concat');
const sourcemaps = require('gulp-sourcemaps');
const cleanCSS = require('gulp-clean-css');
const uglify = require('gulp-uglify');

const appStyles = [
    'Assets/css/site.css'
];

const vendorStyles = [
    'node_modules/bootstrap/dist/css/bootstrap.css',
    'node_modules/highlightjs/styles/default.css'
];

const appScripts = [
    'Assets/js/site.js'
];

const vendorScripts = [
    'node_modules/jquery/dist/jquery.min.js',
    'node_modules/popper.js/dist/umd/popper.min.js',
    'node_modules/bootstrap/dist/js/bootstrap.min.js',
    'node_modules/highlightjs/highlight.pack.min.js'
];

gulp.task('default', ['build-vendor', 'build-app']);

gulp.task('build-vendor', ['build-vendor-css', 'build-vendor-js']);
gulp.task('build-app', ['build-css', 'build-js']);

gulp.task('build-vendor-css', () => {
    return gulp.src(vendorStyles)
        .pipe(sourcemaps.init())
        .pipe(sourcemaps.init())
        .pipe(concat('vendor.min.css'))
        .pipe(cleanCSS())
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest('wwwroot/css/'));
});

gulp.task('build-vendor-js', () => {
    return gulp.src(vendorScripts)
        .pipe(concat('vendor.min.js'))
        .pipe(gulp.dest('wwwroot/js/'));
});

gulp.task('build-css', () => {
    return gulp.src(appStyles)
        .pipe(sourcemaps.init())
        .pipe(concat('site.min.css'))
        .pipe(cleanCSS())
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest('wwwroot/css/'));
});

gulp.task('build-js', () => {
    return gulp.src(appScripts)
        .pipe(sourcemaps.init())
        .pipe(concat('site.min.js'))
        .pipe(uglify())
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest('wwwroot/js/'));
});
