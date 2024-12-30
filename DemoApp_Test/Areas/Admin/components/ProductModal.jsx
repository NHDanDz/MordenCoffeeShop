namespace DemoApp_Test.Areas.Admin.components
{
    public class ProductModal
    {
    import React, { useState, useEffect } from 'react';
    import { Card } from '@/components/ui/card';

    const ProductModal = () => {
        const [modalState, setModalState] = useState({
            productId: '',
            productName: '',
            price: 0,
            discount: 0,
            brandId: '',
            typeId: '',
            rating: 0,
            reviewCount: 0,
            image: null
        });

        useEffect(() => {
            // Initialize modal events
            document.querySelectorAll('.delete-product-btn').forEach(button => {
                button.addEventListener('click', function () {
                    handleDelete(this.dataset.productId, this.dataset.productName);
                });
            });

            document.querySelectorAll('.detail-btn').forEach(button => {
                button.addEventListener('click', function (e) {
                    e.preventDefault();
                    loadProductDetails(this.dataset.productId);
                });
            });
        }, []);

        const handleDelete = async (productId, productName) => {
            if (!confirm(`Are you sure you want to delete ${productName}?`)) return;

            try {
                const response = await fetch(`/Admin/Admin/Delete/${productId}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    }
                });

                if (response.ok) {
                    window.location.reload();
                } else {
                    throw new Error('Delete failed');
                }
            } catch (error) {
                console.error('Error:', error);
                alert('Failed to delete product');
            }
        };

        const loadProductDetails = async (productId) => {
            try {
                const response = await fetch(`/Admin/Product/GetProductDetails/${productId}`);
                const data = await response.json();

                if (data.success) {
                    setModalState({
                        productId: data.data.product_id,
                        productName: data.data.productName,
                        price: data.data.price,
                        discount: data.data.discount,
                        brandId: data.data.brand_id,
                        typeId: data.data.type_id,
                        rating: data.data.rating,
                        reviewCount: data.data.reviewCount,
                        image: `/img/${data.data.brandName}/${data.data.typeName}/${data.data.image}`
                    });

                    const modal = new bootstrap.Modal(document.getElementById('productDetailModal'));
                    modal.show();
                }
            } catch (error) {
                console.error('Error:', error);
                alert('Failed to load product details');
            }
        };

        const handleSubmit = async (event) => {
            event.preventDefault();

            try {
                const formData = new FormData(event.target);
                const response = await fetch('/Admin/Product/UpdateProduct', {
                    method: 'POST',
                    body: formData
                });

                if (response.ok) {
                    window.location.reload();
                } else {
                    throw new Error('Update failed');
                }
            } catch (error) {
                console.error('Error:', error);
                alert('Failed to update product');
            }
        };

        return (
            <div className="modal fade" id="productDetailModal" tabIndex="-1">
                <div className="modal-dialog modal-lg">
                    <div className="modal-content">
                        <div className="modal-header bg-primary text-white">
                            <h5 className="modal-title">Product Details</h5>
                            <button type="button" className="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <form onSubmit={handleSubmit}>
                            <div className="modal-body">
                                <div className="row g-3">
                                    <div className="col-md-6">
                                        <input type="hidden" name="productId" value={modalState.productId} />
                                        <div className="mb-3">
                                            <label className="form-label">Product Name</label>
                                            <input type="text" className="form-control" name="productName"
                                                value={modalState.productName}
                                                onChange={e => setModalState({ ...modalState, productName: e.target.value })} />
                                        </div>
                                        <div className="mb-3">
                                            <label className="form-label">Price</label>
                                            <input type="number" className="form-control" name="price" step="0.01"
                                                value={modalState.price}
                                                onChange={e => setModalState({ ...modalState, price: e.target.value })} />
                                        </div>
                                        <div className="mb-3">
                                            <label className="form-label">Discount (%)</label>
                                            <input type="number" className="form-control" name="discount"
                                                value={modalState.discount}
                                                onChange={e => setModalState({ ...modalState, discount: e.target.value })} />
                                        </div>
                                    </div>
                                    <div className="col-md-6">
                                        <div className="mb-3">
                                            <label className="form-label">Brand</label>
                                            <select className="form-control" name="brandId"
                                                value={modalState.brandId}
                                                onChange={e => setModalState({ ...modalState, brandId: e.target.value })}>
                                                {/* Brand options will be populated from server */}
                                            </select>
                                        </div>
                                        <div className="mb-3">
                                            <label className="form-label">Type</label>
                                            <select className="form-control" name="typeId"
                                                value={modalState.typeId}
                                                onChange={e => setModalState({ ...modalState, typeId: e.target.value })}>
                                                {/* Type options will be populated from server */}
                                            </select>
                                        </div>
                                        <div className="mb-3">
                                            <label className="form-label">Rating</label>
                                            <input type="number" className="form-control" name="rating"
                                                value={modalState.rating}
                                                onChange={e => setModalState({ ...modalState, rating: e.target.value })} />
                                        </div>
                                        <div className="mb-3">
                                            <label className="form-label">Review Count</label>
                                            <input type="number" className="form-control" name="reviewCount"
                                                value={modalState.reviewCount}
                                                onChange={e => setModalState({ ...modalState, reviewCount: e.target.value })} />
                                        </div>
                                    </div>
                                </div>
                                <div className="mb-3">
                                    <label className="form-label">Product Image</label>
                                    <input type="file" className="form-control" name="newImage" accept="image/*" />
                                    {modalState.image && (
                                        <img src={modalState.image} alt="Product" className="mt-2" style={{ maxHeight: '200px' }} />
                                    )}
                                </div>
                            </div>
                            <div className="modal-footer">
                                <button type="button" className="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                                <button type="submit" className="btn btn-primary">Save changes</button>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        );
    };

    export default ProductModal;
    }
}
